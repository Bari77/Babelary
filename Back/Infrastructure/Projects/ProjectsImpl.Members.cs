using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Core.Accounts;
using Core.Exceptions;
using Core.Projects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Projects;

public partial class ProjectsImpl
{
    private static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(7);

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileProgressDto>> ListFileProgressAsync(
        Guid accountId,
        string? project,
        string? locale,
        CancellationToken token)
    {
        var stored = await RequireVisibleAsync(accountId, project, token);
        var visible = await VisibleLocalesAsync(stored, accountId, token);
        var code = NormalizeLocale(locale);
        if (!visible.Contains(code))
        {
            throw new ForbiddenException("ERR_PROJECT_FORBIDDEN");
        }

        var files = await db.ProjectFiles
            .AsNoTracking()
            .Where(file => file.ProjectId == stored.Id)
            .OrderBy(file => file.Name)
            .Select(file => new { file.Id, file.Name })
            .ToListAsync(token);
        var totals = await db.ProjectKeys
            .Where(key => key.File.ProjectId == stored.Id)
            .GroupBy(key => key.FileId)
            .Select(group => new { FileId = group.Key, Count = group.Count() })
            .ToListAsync(token);
        var translated = await db.ProjectValues
            .Where(value => value.Locale == code && value.Text != "" && value.Key.File.ProjectId == stored.Id)
            .GroupBy(value => value.Key.FileId)
            .Select(group => new { FileId = group.Key, Count = group.Count() })
            .ToListAsync(token);
        var pending = await db.ProjectProposals
            .Where(proposal => proposal.Status == ProjectProposal.Pending
                && proposal.Locale == code
                && proposal.Key.File.ProjectId == stored.Id)
            .GroupBy(proposal => proposal.Key.FileId)
            .Select(group => new { FileId = group.Key, Count = group.Count() })
            .ToListAsync(token);
        var totalByFile = totals.ToDictionary(row => row.FileId, row => row.Count);
        var translatedByFile = translated.ToDictionary(row => row.FileId, row => row.Count);
        var pendingOpen = await db.ProjectProposals
            .Where(proposal => proposal.Status == ProjectProposal.Pending
                && proposal.Locale == code
                && proposal.Key.File.ProjectId == stored.Id
                && !proposal.Key.Values.Any(value => value.Locale == code && value.Text != ""))
            .GroupBy(proposal => proposal.Key.FileId)
            .Select(group => new { FileId = group.Key, Count = group.Count() })
            .ToListAsync(token);
        var pendingByFile = pending.ToDictionary(row => row.FileId, row => row.Count);
        var pendingOpenByFile = pendingOpen.ToDictionary(row => row.FileId, row => row.Count);
        return files
            .Select(file => new FileProgressDto
            {
                Name = file.Name,
                Total = totalByFile.GetValueOrDefault(file.Id),
                Translated = translatedByFile.GetValueOrDefault(file.Id),
                Pending = pendingByFile.GetValueOrDefault(file.Id),
                PendingOpen = pendingOpenByFile.GetValueOrDefault(file.Id),
            })
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProjectMemberDto>> ListMembersAsync(Guid accountId, string? project, CancellationToken token)
    {
        var stored = await RequireOwnedAsync(accountId, project, token);
        var members = await db.ProjectMembers
            .AsNoTracking()
            .Include(member => member.Account)
            .Include(member => member.Locales)
            .Where(member => member.ProjectId == stored.Id)
            .OrderBy(member => member.Account.Name)
            .ToListAsync(token);
        return members.Select(ToMemberDto).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProjectInviteDto>> ListInvitesAsync(Guid accountId, string? project, CancellationToken token)
    {
        var stored = await RequireOwnedAsync(accountId, project, token);
        var now = DateTime.UtcNow;
        var invites = await db.ProjectInvites
            .AsNoTracking()
            .Include(invite => invite.Locales)
            .Where(invite => invite.ProjectId == stored.Id
                && invite.AcceptedAt == null
                && invite.RevokedAt == null
                && invite.ExpiresAt > now)
            .OrderBy(invite => invite.Email)
            .ToListAsync(token);
        return invites.Select(ToInviteDto).ToArray();
    }

    /// <inheritdoc />
    public async Task<CreatedInviteDto> InviteAsync(
        Guid accountId,
        string? project,
        string? email,
        IReadOnlyList<string?>? locales,
        CancellationToken token)
    {
        var stored = await RequireOwnedAsync(accountId, project, token);
        var normalizedEmail = EmailAddress.Normalize(email) ?? throw new BadRequestException("ERR_EMAIL_INVALID");
        var owner = await db.Accounts.AsNoTracking().FirstAsync(account => account.Id == stored.OwnerAccountId, token);
        if (string.Equals(owner.Email, normalizedEmail, StringComparison.Ordinal))
        {
            throw new ConflictException("ERR_INVITE_OWNER");
        }

        var assigned = NormalizeMemberLocales(locales, stored);
        var code = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var now = DateTime.UtcNow;
        var invite = new ProjectInvite
        {
            ProjectId = stored.Id,
            Email = normalizedEmail,
            TokenHash = HashSecret(code),
            ExpiresAt = now.Add(InviteLifetime),
            CreatedAt = now,
        };
        foreach (var locale in assigned)
        {
            invite.Locales.Add(new ProjectInviteLocale { Locale = locale });
        }

        db.ProjectInvites.Add(invite);
        await db.SaveChangesAsync(token);
        return new CreatedInviteDto
        {
            Id = invite.Id,
            Email = invite.Email,
            Locales = assigned,
            ExpiresAt = invite.ExpiresAt,
            Code = code,
        };
    }

    /// <inheritdoc />
    public async Task RevokeInviteAsync(Guid accountId, string? project, Guid inviteId, CancellationToken token)
    {
        var stored = await RequireOwnedAsync(accountId, project, token);
        var invite = await db.ProjectInvites.FirstOrDefaultAsync(
            item => item.Id == inviteId && item.ProjectId == stored.Id && item.AcceptedAt == null && item.RevokedAt == null,
            token);
        if (invite is null)
        {
            throw new NotFoundException("ERR_NOT_FOUND");
        }

        invite.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(token);
    }

    /// <inheritdoc />
    public async Task<ProjectMemberDto> UpdateMemberAsync(
        Guid accountId,
        string? project,
        Guid memberAccountId,
        IReadOnlyList<string?>? locales,
        CancellationToken token)
    {
        var stored = await RequireOwnedAsync(accountId, project, token);
        var assigned = NormalizeMemberLocales(locales, stored);
        var member = await db.ProjectMembers
            .Include(item => item.Account)
            .Include(item => item.Locales)
            .FirstOrDefaultAsync(item => item.ProjectId == stored.Id && item.AccountId == memberAccountId, token);
        if (member is null)
        {
            throw new NotFoundException("ERR_NOT_FOUND");
        }

        var wanted = assigned.ToHashSet(StringComparer.Ordinal);
        foreach (var locale in member.Locales.Where(locale => !wanted.Contains(locale.Locale)).ToList())
        {
            member.Locales.Remove(locale);
        }

        var owned = member.Locales.Select(locale => locale.Locale).ToHashSet(StringComparer.Ordinal);
        foreach (var locale in assigned.Where(locale => !owned.Contains(locale)))
        {
            member.Locales.Add(new ProjectMemberLocale { Locale = locale });
        }

        await db.SaveChangesAsync(token);
        return ToMemberDto(member);
    }

    /// <inheritdoc />
    public async Task RemoveMemberAsync(Guid accountId, string? project, Guid memberAccountId, CancellationToken token)
    {
        var stored = await RequireOwnedAsync(accountId, project, token);
        var member = await db.ProjectMembers.FirstOrDefaultAsync(
            item => item.ProjectId == stored.Id && item.AccountId == memberAccountId,
            token);
        if (member is null)
        {
            throw new NotFoundException("ERR_NOT_FOUND");
        }

        db.ProjectMembers.Remove(member);
        await db.SaveChangesAsync(token);
    }

    /// <inheritdoc />
    public async Task<InvitePreviewDto> PreviewInviteAsync(string? code, CancellationToken token)
        => await ToPreviewAsync(await ReadInviteAsync(code, token), token);

    /// <inheritdoc />
    public async Task<AccountDto> AcceptInviteAsync(
        string? code,
        Guid? signedInAccountId,
        string? name,
        string? password,
        CancellationToken token)
    {
        var invite = await ReadInviteAsync(code, token);
        var locales = OpenLocales(invite);
        var existing = await db.Accounts.FirstOrDefaultAsync(account => account.Email == invite.Email, token);
        Account account;
        if (existing is null)
        {
            if (!PasswordRules.IsValid(password))
            {
                throw new BadRequestException("ERR_PASSWORD_TOO_SHORT");
            }

            account = new Account
            {
                Name = NormalizeName(name),
                Email = invite.Email,
                IsInstanceOwner = false,
                CreatedAt = DateTime.UtcNow,
            };
            account.PasswordHash = passwords.HashPassword(account, password!);
            db.Accounts.Add(account);
        }
        else if (signedInAccountId is null)
        {
            throw new UnauthorizedException("ERR_INVITE_LOGIN");
        }
        else if (signedInAccountId != existing.Id)
        {
            throw new ForbiddenException("ERR_INVITE_ACCOUNT");
        }
        else
        {
            account = existing;
        }

        await AttachMemberAsync(invite.ProjectId, account, locales, token);
        invite.AcceptedAt = DateTime.UtcNow;
        try
        {
            await db.SaveChangesAsync(token);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("ERR_EMAIL_TAKEN");
        }

        return new AccountDto
        {
            Id = account.Id,
            Name = account.Name,
            Email = account.Email,
            IsInstanceOwner = account.IsInstanceOwner,
        };
    }

    private async Task<InvitePreviewDto> ToPreviewAsync(ProjectInvite invite, CancellationToken token)
    {
        return new InvitePreviewDto
        {
            ProjectName = invite.Project.Name,
            ProjectSlug = invite.Project.Slug,
            Email = invite.Email,
            Locales = OpenLocales(invite),
            AccountExists = await db.Accounts.AnyAsync(account => account.Email == invite.Email, token),
        };
    }

    private async Task<ProjectInvite> ReadInviteAsync(string? code, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new NotFoundException("ERR_INVITE_INVALID");
        }

        var invite = await db.ProjectInvites
            .Include(item => item.Project)
            .ThenInclude(project => project.Targets)
            .Include(item => item.Locales)
            .FirstOrDefaultAsync(item => item.TokenHash == HashSecret(code), token);
        if (invite is null || invite.RevokedAt != null || invite.AcceptedAt != null || invite.ExpiresAt <= DateTime.UtcNow)
        {
            throw new NotFoundException("ERR_INVITE_INVALID");
        }

        if (OpenLocales(invite).Length == 0)
        {
            throw new NotFoundException("ERR_INVITE_INVALID");
        }

        return invite;
    }

    private static string[] OpenLocales(ProjectInvite invite)
    {
        var targets = invite.Project.Targets.Select(target => target.Locale).ToHashSet(StringComparer.Ordinal);
        return invite.Locales
            .Select(locale => locale.Locale)
            .Where(targets.Contains)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(locale => locale, StringComparer.Ordinal)
            .ToArray();
    }

    private async Task AttachMemberAsync(Guid projectId, Account account, IReadOnlyList<string> locales, CancellationToken token)
    {
        var member = account.Id == Guid.Empty
            ? null
            : await db.ProjectMembers
                .Include(item => item.Locales)
                .FirstOrDefaultAsync(item => item.ProjectId == projectId && item.AccountId == account.Id, token);
        if (member is null)
        {
            member = new ProjectMember
            {
                ProjectId = projectId,
                Account = account,
                CreatedAt = DateTime.UtcNow,
            };
            db.ProjectMembers.Add(member);
        }

        var owned = member.Locales.Select(locale => locale.Locale).ToHashSet(StringComparer.Ordinal);
        foreach (var locale in locales.Where(locale => !owned.Contains(locale)))
        {
            member.Locales.Add(new ProjectMemberLocale { Locale = locale });
        }
    }

    private static string[] NormalizeMemberLocales(IReadOnlyList<string?>? locales, Project project)
    {
        var assigned = NormalizeTargets(locales, project.SourceLocale);
        var targets = project.Targets.Select(target => target.Locale).ToHashSet(StringComparer.Ordinal);
        if (assigned.Any(locale => !targets.Contains(locale)))
        {
            throw new NotFoundException("ERR_TARGET_MISSING");
        }

        return assigned.OrderBy(locale => locale, StringComparer.Ordinal).ToArray();
    }

    private static ProjectMemberDto ToMemberDto(ProjectMember member) => new()
    {
        AccountId = member.AccountId,
        Name = member.Account.Name,
        Email = member.Account.Email,
        Locales = member.Locales.Select(locale => locale.Locale).OrderBy(locale => locale, StringComparer.Ordinal).ToArray(),
    };

    private static ProjectInviteDto ToInviteDto(ProjectInvite invite) => new()
    {
        Id = invite.Id,
        Email = invite.Email,
        Locales = invite.Locales.Select(locale => locale.Locale).OrderBy(locale => locale, StringComparer.Ordinal).ToArray(),
        ExpiresAt = invite.ExpiresAt,
    };

    private static string HashSecret(string secret)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
}
