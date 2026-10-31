using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Core.Accounts;
using Core.Exceptions;
using Core.Locales;
using Core.Projects;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Projects;

/// <summary>
/// Projects stored in PostgreSQL. A member who does not own a project cannot see it.
/// </summary>
public partial class ProjectsImpl(BabelaryDbContext db, IPasswordHasher<Account> passwords) : IProjects
{
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*\\.json$")]
    private static partial Regex FileNamePattern();

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProjectDto>> ListAsync(Guid accountId, CancellationToken token)
    {
        var projects = await db.Projects
            .AsNoTracking()
            .Include(project => project.Targets)
            .Where(project => project.OwnerAccountId == accountId
                || project.Members.Any(member => member.AccountId == accountId))
            .OrderBy(project => project.Name)
            .ToListAsync(token);

        var dtos = new List<ProjectDto>(projects.Count);
        foreach (var project in projects)
        {
            dtos.Add(await CompleteAsync(project, accountId, token));
        }

        return dtos;
    }

    /// <inheritdoc />
    public async Task<ProjectDto> GetAsync(Guid accountId, string? project, CancellationToken token)
    {
        return await CompleteAsync(await RequireVisibleAsync(accountId, project, token), accountId, token);
    }

    /// <inheritdoc />
    public async Task<ProjectDto> CreateAsync(
        Guid accountId,
        string? slug,
        string? name,
        string? sourceLocale,
        CancellationToken token)
    {
        var owner = await db.Accounts.AsNoTracking().FirstOrDefaultAsync(account => account.Id == accountId, token)
            ?? throw new NotFoundException("ERR_NOT_FOUND");
        if (!owner.IsInstanceOwner)
        {
            throw new ForbiddenException("ERR_FORBIDDEN");
        }

        var normalizedSlug = NormalizeSlug(slug);
        var displayName = NormalizeName(name);
        var locale = NormalizeLocale(sourceLocale);
        if (await db.Projects.AnyAsync(project => project.Slug == normalizedSlug, token))
        {
            throw new ConflictException("ERR_PROJECT_SLUG_TAKEN");
        }

        var now = DateTime.UtcNow;
        var created = new Project
        {
            Id = Guid.NewGuid(),
            OwnerAccountId = accountId,
            Slug = normalizedSlug,
            Name = displayName,
            SourceLocale = locale,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Projects.Add(created);
        try
        {
            await db.SaveChangesAsync(token);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("ERR_PROJECT_SLUG_TAKEN");
        }

        return await CompleteAsync(created, accountId, token);
    }

    /// <inheritdoc />
    public async Task<ProjectDto> UpdateAsync(
        Guid accountId,
        string? project,
        string? name,
        string? sourceLocale,
        CancellationToken token)
    {
        var stored = await RequireOwnedAsync(accountId, project, token);
        var displayName = NormalizeName(name);
        var locale = NormalizeLocale(sourceLocale);
        if (!string.Equals(stored.SourceLocale, locale, StringComparison.Ordinal)
            && await HasKeysAsync(stored.Id, token))
        {
            throw new ConflictException("ERR_SOURCE_LOCALE_LOCKED");
        }

        if (stored.Targets.Any(target => target.Locale == locale))
        {
            throw new ConflictException("ERR_TARGET_IS_SOURCE");
        }

        stored.Name = displayName;
        stored.SourceLocale = locale;
        stored.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(token);
        return await CompleteAsync(stored, accountId, token);
    }

    /// <inheritdoc />
    public async Task<ProjectDto> ResetAsync(Guid accountId, string? project, CancellationToken token)
    {
        var stored = await RequireOwnedAsync(accountId, project, token);
        await DeleteSourcesAsync(stored.Id, token);
        stored.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(token);
        return await CompleteAsync(stored, accountId, token);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid accountId, string? project, CancellationToken token)
    {
        var stored = await RequireOwnedAsync(accountId, project, token);
        db.Projects.Remove(stored);
        await db.SaveChangesAsync(token);
    }

    private async Task DeleteSourcesAsync(Guid projectId, CancellationToken token)
    {
        await db.ProjectProposals.Where(proposal => proposal.Key.File.ProjectId == projectId).ExecuteDeleteAsync(token);
        await db.ProjectValues.Where(value => value.Key.File.ProjectId == projectId).ExecuteDeleteAsync(token);
        await db.ProjectKeys.Where(key => key.File.ProjectId == projectId).ExecuteDeleteAsync(token);
        await db.ProjectFiles.Where(file => file.ProjectId == projectId).ExecuteDeleteAsync(token);
    }

    /// <inheritdoc />
    public async Task<ProjectDto> AddTargetsAsync(
        Guid accountId,
        string? project,
        IReadOnlyList<string?>? locales,
        CancellationToken token)
    {
        var stored = await RequireOwnedAsync(accountId, project, token);
        var adding = NormalizeTargets(locales, stored.SourceLocale);
        var owned = stored.Targets.Select(target => target.Locale).ToHashSet(StringComparer.Ordinal);
        if (adding.Any(owned.Contains))
        {
            throw new ConflictException("ERR_TARGET_EXISTS");
        }

        var now = DateTime.UtcNow;
        foreach (var locale in adding)
        {
            stored.Targets.Add(new ProjectTarget
            {
                Locale = locale,
                CreatedAt = now,
            });
        }

        var keys = await db.ProjectKeys
            .Where(key => key.File.ProjectId == stored.Id)
            .Select(key => new { key.Id, key.SourceValue })
            .ToListAsync(token);
        foreach (var locale in adding)
        {
            foreach (var key in keys)
            {
                db.ProjectValues.Add(new ProjectValue
                {
                    KeyId = key.Id,
                    Locale = locale,
                    Text = "",
                    SourceSnapshot = key.SourceValue,
                    UpdatedAt = now,
                });
            }
        }

        stored.UpdatedAt = now;
        await db.SaveChangesAsync(token);
        return await CompleteAsync(stored, accountId, token);
    }

    /// <inheritdoc />
    public async Task<ProjectDto> RemoveTargetsAsync(
        Guid accountId,
        string? project,
        IReadOnlyList<string?>? locales,
        CancellationToken token)
    {
        var stored = await RequireOwnedAsync(accountId, project, token);
        var removing = NormalizeTargets(locales, stored.SourceLocale);
        var owned = stored.Targets.Select(target => target.Locale).ToHashSet(StringComparer.Ordinal);
        if (removing.Any(locale => !owned.Contains(locale)))
        {
            throw new NotFoundException("ERR_TARGET_MISSING");
        }

        await db.ProjectValues
            .Where(value => removing.Contains(value.Locale) && value.Key.File.ProjectId == stored.Id)
            .ExecuteDeleteAsync(token);
        foreach (var target in stored.Targets.Where(target => removing.Contains(target.Locale)).ToList())
        {
            stored.Targets.Remove(target);
        }

        stored.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(token);
        return await CompleteAsync(stored, accountId, token);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProjectFileDto>> ListFilesAsync(Guid accountId, string? project, CancellationToken token)
    {
        var stored = await RequireOwnedAsync(accountId, project, token);
        return await db.ProjectFiles
            .AsNoTracking()
            .Where(file => file.ProjectId == stored.Id)
            .OrderBy(file => file.Name)
            .Select(file => new ProjectFileDto { Name = file.Name, KeyCount = file.Keys.Count })
            .ToListAsync(token);
    }

    /// <inheritdoc />
    public async Task<ProjectDto> DeleteFileAsync(Guid accountId, string? project, string? file, CancellationToken token)
    {
        var stored = await RequireOwnedAsync(accountId, project, token);
        var name = file?.Trim() ?? "";
        if (name.Length == 0 || name.Length > 80 || name.Contains("..", StringComparison.Ordinal) || !FileNamePattern().IsMatch(name))
        {
            throw new BadRequestException("ERR_FILE_NAME_INVALID");
        }

        var fileId = await db.ProjectFiles
            .Where(item => item.ProjectId == stored.Id && item.Name == name)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(token);
        if (fileId is null)
        {
            throw new NotFoundException("ERR_FILE_MISSING");
        }

        await DeleteFilesByIdAsync([fileId.Value], token);
        stored.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(token);
        return await CompleteAsync(stored, accountId, token);
    }

    /// <inheritdoc />
    public async Task<ImportSourcesDto> ImportSourcesAsync(
        Guid accountId,
        string? project,
        IReadOnlyList<SourceFileUpload>? files,
        CancellationToken token,
        bool deleteMissingFiles = false)
    {
        var stored = await RequireOwnedAsync(accountId, project, token);
        if (files is null || files.Count == 0)
        {
            throw new BadRequestException("ERR_FILE_REQUIRED");
        }

        var now = DateTime.UtcNow;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var results = new List<ImportFileDto>(files.Count);
        foreach (var file in files)
        {
            results.Add(await ImportOneAsync(stored, file, seen, now, token));
        }

        var deletedFiles = 0;
        if (deleteMissingFiles && results.Any(result => result.Error is null))
        {
            var kept = results
                .Where(result => result.Error != "ERR_FILE_NAME_INVALID")
                .Select(result => result.Name)
                .ToHashSet(StringComparer.Ordinal);
            deletedFiles = await DeleteUnmentionedFilesAsync(stored.Id, kept, token);
        }

        stored.UpdatedAt = now;
        await db.SaveChangesAsync(token);
        return new ImportSourcesDto
        {
            Files = results.ToArray(),
            Project = await CompleteAsync(stored, accountId, token),
            DeletedFiles = deletedFiles,
        };
    }

    private async Task<int> DeleteUnmentionedFilesAsync(Guid projectId, HashSet<string> kept, CancellationToken token)
    {
        var staleIds = await db.ProjectFiles
            .Where(file => file.ProjectId == projectId && !kept.Contains(file.Name))
            .Select(file => file.Id)
            .ToListAsync(token);
        if (staleIds.Count == 0)
        {
            return 0;
        }

        await DeleteFilesByIdAsync(staleIds, token);
        return staleIds.Count;
    }

    private async Task DeleteFilesByIdAsync(IReadOnlyList<Guid> fileIds, CancellationToken token)
    {
        if (fileIds.Count == 0)
        {
            return;
        }

        var keyIds = await db.ProjectKeys
            .Where(key => fileIds.Contains(key.FileId))
            .Select(key => key.Id)
            .ToListAsync(token);
        if (keyIds.Count > 0)
        {
            await db.ProjectProposals.Where(proposal => keyIds.Contains(proposal.KeyId)).ExecuteDeleteAsync(token);
            await db.ProjectValues.Where(value => keyIds.Contains(value.KeyId)).ExecuteDeleteAsync(token);
            await db.ProjectKeys.Where(key => fileIds.Contains(key.FileId)).ExecuteDeleteAsync(token);
        }

        await db.ProjectFiles.Where(file => fileIds.Contains(file.Id)).ExecuteDeleteAsync(token);
    }

    private async Task<ImportFileDto> ImportOneAsync(
        Project stored,
        SourceFileUpload? file,
        HashSet<string> seen,
        DateTime now,
        CancellationToken token)
    {
        var name = file?.Name?.Trim() ?? "";
        if (name.Length == 0 || name.Length > 80 || name.Contains("..", StringComparison.Ordinal) || !FileNamePattern().IsMatch(name))
        {
            return new ImportFileDto { Name = name, Error = "ERR_FILE_NAME_INVALID" };
        }

        if (!seen.Add(name))
        {
            return new ImportFileDto { Name = name, Error = "ERR_FILE_NAME_INVALID" };
        }

        ParsedDocument parsed;
        try
        {
            parsed = SourceDocument.Parse(file?.Content, allowFlatKeys: false);
        }
        catch (BadRequestException exception)
        {
            return new ImportFileDto { Name = name, Error = exception.Message };
        }

        var existing = await db.ProjectFiles
            .Include(item => item.Keys)
            .ThenInclude(key => key.Values)
            .FirstOrDefaultAsync(item => item.ProjectId == stored.Id && item.Name == name, token);
        if (existing is null)
        {
            existing = new ProjectFile
            {
                ProjectId = stored.Id,
                Name = name,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.ProjectFiles.Add(existing);
        }

        existing.Catalog = parsed.Angular ? ProjectFile.AngularCatalog : ProjectFile.NestedCatalog;
        var byPath = existing.Keys.ToDictionary(key => key.Path, StringComparer.Ordinal);
        var created = 0;
        var updated = 0;
        var ordinal = 0;
        foreach (var (path, source) in parsed.Leaves)
        {
            if (!byPath.Remove(path, out var key))
            {
                key = new ProjectKey
                {
                    Path = path,
                    SourceValue = source,
                    Ordinal = ordinal,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                foreach (var target in stored.Targets)
                {
                    key.Values.Add(new ProjectValue
                    {
                        Locale = target.Locale,
                        Text = "",
                        SourceSnapshot = source,
                        UpdatedAt = now,
                    });
                }

                existing.Keys.Add(key);
                created++;
            }
            else
            {
                key.Ordinal = ordinal;
                if (!string.Equals(key.SourceValue, source, StringComparison.Ordinal))
                {
                    key.SourceValue = source;
                    key.UpdatedAt = now;
                    updated++;
                }
            }

            ordinal++;
        }

        foreach (var missing in byPath.Values)
        {
            existing.Keys.Remove(missing);
        }

        existing.UpdatedAt = now;
        return new ImportFileDto
        {
            Name = name,
            Created = created,
            Updated = updated,
            Deleted = byPath.Count,
        };
    }

    private Task<bool> HasKeysAsync(Guid projectId, CancellationToken token)
        => db.ProjectKeys.AnyAsync(key => key.File.ProjectId == projectId, token);

    private async Task<Project> RequireVisibleAsync(Guid accountId, string? project, CancellationToken token)
    {
        var stored = await FindProjectAsync(project, token);
        if (stored.OwnerAccountId == accountId)
        {
            return stored;
        }

        var member = await db.ProjectMembers.AnyAsync(
            item => item.ProjectId == stored.Id && item.AccountId == accountId,
            token);
        if (!member)
        {
            throw new NotFoundException("ERR_NOT_FOUND");
        }

        return stored;
    }

    private async Task<Project> RequireOwnedAsync(Guid accountId, string? project, CancellationToken token)
    {
        var stored = await FindProjectAsync(project, token);
        if (stored.OwnerAccountId == accountId)
        {
            return stored;
        }

        var member = await db.ProjectMembers.AnyAsync(
            item => item.ProjectId == stored.Id && item.AccountId == accountId,
            token);
        throw member ? new ForbiddenException("ERR_PROJECT_FORBIDDEN") : new NotFoundException("ERR_NOT_FOUND");
    }

    private async Task<Project> FindProjectAsync(string? project, CancellationToken token)
    {
        var slug = project?.Trim() ?? "";
        if (slug.Length == 0)
        {
            throw new NotFoundException("ERR_NOT_FOUND");
        }

        var stored = await db.Projects
            .Include(item => item.Targets)
            .FirstOrDefaultAsync(item => item.Slug == slug, token);
        return stored ?? throw new NotFoundException("ERR_NOT_FOUND");
    }

    private static string NormalizeSlug(string? slug)
    {
        var trimmed = slug?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            throw new BadRequestException("ERR_PROJECT_SLUG_REQUIRED");
        }

        if (trimmed.Length > 80 || !SlugPattern().IsMatch(trimmed))
        {
            throw new BadRequestException("ERR_PROJECT_SLUG_INVALID");
        }

        return trimmed;
    }

    private static string NormalizeName(string? name)
    {
        var trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            throw new BadRequestException("ERR_NAME_REQUIRED");
        }

        if (trimmed.Length > 120)
        {
            throw new BadRequestException("ERR_NAME_TOO_LONG");
        }

        return trimmed;
    }

    private static string NormalizeLocale(string? sourceLocale)
    {
        var trimmed = sourceLocale?.Trim() ?? "";
        if (!LocaleCatalog.Contains(trimmed))
        {
            throw new BadRequestException("ERR_LOCALE_INVALID");
        }

        return trimmed;
    }

    private static string[] NormalizeTargets(IReadOnlyList<string?>? locales, string sourceLocale)
    {
        if (locales is null || locales.Count == 0)
        {
            throw new BadRequestException("ERR_TARGET_REQUIRED");
        }

        var normalized = locales.Select(NormalizeLocale).Distinct(StringComparer.Ordinal).ToArray();
        if (normalized.Contains(sourceLocale, StringComparer.Ordinal))
        {
            throw new ConflictException("ERR_TARGET_IS_SOURCE");
        }

        return normalized;
    }

    private static ProjectDto ToDto(Project project)
    {
        var targets = project.Targets
            .Select(target => target.Locale)
            .OrderBy(locale => locale, StringComparer.Ordinal)
            .ToArray();
        return new ProjectDto
        {
            Slug = project.Slug,
            Name = project.Name,
            SourceLocale = project.SourceLocale,
            Targets = targets,
            TargetCount = targets.Length,
            Progress = [],
            Translated = 0,
            Total = 0,
            SourceLocaleLocked = false,
        };
    }

    private async Task<ProjectDto> CompleteAsync(Project project, Guid accountId, CancellationToken token)
    {
        var dto = ToDto(project);
        var visible = await VisibleLocalesAsync(project, accountId, token);
        dto.Targets = dto.Targets.Where(visible.Contains).ToArray();
        dto.TargetCount = dto.Targets.Length;
        var keyCount = await db.ProjectKeys.CountAsync(key => key.File.ProjectId == project.Id, token);
        dto.SourceLocaleLocked = keyCount > 0;
        var translated = await db.ProjectValues
            .Where(value => value.Text != "" && value.Key.File.ProjectId == project.Id && visible.Contains(value.Locale))
            .GroupBy(value => value.Locale)
            .Select(group => new { Locale = group.Key, Count = group.Count() })
            .ToListAsync(token);
        var pending = await db.ProjectProposals
            .Where(proposal => proposal.Status == ProjectProposal.Pending
                && proposal.Key.File.ProjectId == project.Id
                && visible.Contains(proposal.Locale))
            .GroupBy(proposal => proposal.Locale)
            .Select(group => new { Locale = group.Key, Count = group.Count() })
            .ToListAsync(token);
        var byLocale = translated.ToDictionary(row => row.Locale, row => row.Count, StringComparer.Ordinal);
        var pendingOpen = await db.ProjectProposals
            .Where(proposal => proposal.Status == ProjectProposal.Pending
                && proposal.Key.File.ProjectId == project.Id
                && visible.Contains(proposal.Locale)
                && !proposal.Key.Values.Any(value => value.Locale == proposal.Locale && value.Text != ""))
            .GroupBy(proposal => proposal.Locale)
            .Select(group => new { Locale = group.Key, Count = group.Count() })
            .ToListAsync(token);
        var pendingByLocale = pending.ToDictionary(row => row.Locale, row => row.Count, StringComparer.Ordinal);
        var pendingOpenByLocale = pendingOpen.ToDictionary(row => row.Locale, row => row.Count, StringComparer.Ordinal);
        dto.Progress = dto.Targets
            .Select(locale => new LocaleProgressDto
            {
                Locale = locale,
                Translated = byLocale.GetValueOrDefault(locale),
                Total = keyCount,
                Pending = pendingByLocale.GetValueOrDefault(locale),
                PendingOpen = pendingOpenByLocale.GetValueOrDefault(locale),
            })
            .ToArray();
        dto.Translated = dto.Progress.Sum(row => row.Translated);
        dto.Pending = dto.Progress.Sum(row => row.Pending);
        dto.PendingOpen = dto.Progress.Sum(row => row.PendingOpen);
        dto.Total = keyCount * dto.Targets.Length;
        return dto;
    }

    private async Task<HashSet<string>> VisibleLocalesAsync(Project project, Guid accountId, CancellationToken token)
    {
        if (project.OwnerAccountId == accountId)
        {
            return project.Targets.Select(target => target.Locale).ToHashSet(StringComparer.Ordinal);
        }

        var assigned = await db.ProjectMemberLocales
            .Where(locale => locale.Member.ProjectId == project.Id && locale.Member.AccountId == accountId)
            .Select(locale => locale.Locale)
            .ToListAsync(token);
        var targets = project.Targets.Select(target => target.Locale).ToHashSet(StringComparer.Ordinal);
        return assigned.Where(targets.Contains).ToHashSet(StringComparer.Ordinal);
    }
}
