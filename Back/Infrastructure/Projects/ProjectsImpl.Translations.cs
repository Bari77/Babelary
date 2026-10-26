using Core.Exceptions;
using Core.Projects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Projects;

public partial class ProjectsImpl
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TranslationEntryDto>> ListEntriesAsync(
        Guid accountId,
        string? project,
        string? locale,
        string? file,
        CancellationToken token)
    {
        var stored = await RequireVisibleAsync(accountId, project, token);
        var code = await RequireVisibleLocaleAsync(stored, accountId, locale, token);
        var source = await RequireFileAsync(stored.Id, file, token);
        return await LoadEntriesAsync(source.Id, code, token);
    }

    /// <inheritdoc />
    public async Task<TranslationEntryDto> SaveTranslationAsync(
        Guid accountId,
        string? project,
        string? locale,
        Guid keyId,
        string? text,
        CancellationToken token)
    {
        var stored = await RequireVisibleAsync(accountId, project, token);
        var code = await RequireVisibleLocaleAsync(stored, accountId, locale, token);
        var key = await db.ProjectKeys
            .Include(item => item.Values)
            .FirstOrDefaultAsync(item => item.Id == keyId && item.File.ProjectId == stored.Id, token)
            ?? throw new NotFoundException("ERR_NOT_FOUND");
        var next = text ?? "";
        if (next.Length > 0)
        {
            Placeholders.RequireSame(key.SourceValue, next);
        }

        var proposal = await db.ProjectProposals
            .Include(item => item.Author)
            .FirstOrDefaultAsync(item => item.KeyId == key.Id && item.Locale == code, token);
        if (stored.OwnerAccountId == accountId)
        {
            ApplyValue(key, code, next);
            if (proposal is not null)
            {
                db.ProjectProposals.Remove(proposal);
                proposal = null;
            }
        }
        else
        {
            var author = await db.Accounts.FirstAsync(account => account.Id == accountId, token);
            if (proposal is null)
            {
                proposal = new ProjectProposal
                {
                    KeyId = key.Id,
                    Locale = code,
                    AuthorAccountId = accountId,
                    Author = author,
                };
                db.ProjectProposals.Add(proposal);
            }

            proposal.Text = next;
            proposal.Status = ProjectProposal.Pending;
            proposal.AuthorAccountId = accountId;
            proposal.Author = author;
            proposal.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(token);
        var value = key.Values.FirstOrDefault(item => item.Locale == code);
        return ToEntry(key, value, proposal);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProposalDto>> ListProposalsAsync(
        Guid accountId,
        string? project,
        string? locale,
        CancellationToken token)
    {
        var stored = await RequireOwnedAsync(accountId, project, token);
        var code = await VisibleCodeAsync(stored, accountId, locale, token);
        return await LoadPendingAsync(stored.Id, code, token);
    }

    /// <inheritdoc />
    public Task<ProposalBatchDto> ApproveProposalsAsync(
        Guid accountId,
        string? project,
        string? locale,
        IReadOnlyList<Guid>? ids,
        CancellationToken token)
        => DecideAsync(accountId, project, locale, ids, approve: true, token);

    /// <inheritdoc />
    public Task<ProposalBatchDto> RefuseProposalsAsync(
        Guid accountId,
        string? project,
        string? locale,
        IReadOnlyList<Guid>? ids,
        CancellationToken token)
        => DecideAsync(accountId, project, locale, ids, approve: false, token);

    private async Task<ProposalBatchDto> DecideAsync(
        Guid accountId,
        string? project,
        string? locale,
        IReadOnlyList<Guid>? ids,
        bool approve,
        CancellationToken token)
    {
        var stored = await RequireOwnedAsync(accountId, project, token);
        var code = await VisibleCodeAsync(stored, accountId, locale, token);
        var requested = (ids ?? []).Where(id => id != Guid.Empty).Distinct().ToArray();
        if (requested.Length == 0)
        {
            throw new BadRequestException("ERR_PROPOSAL_REQUIRED");
        }

        var proposals = await db.ProjectProposals
            .Include(item => item.Key)
            .ThenInclude(key => key.Values)
            .Include(item => item.Key)
            .ThenInclude(key => key.File)
            .Where(item => requested.Contains(item.Id)
                && (code == null || item.Locale == code)
                && item.Status == ProjectProposal.Pending
                && item.Key.File.ProjectId == stored.Id)
            .ToListAsync(token);
        var byId = proposals.ToDictionary(item => item.Id);
        var results = new List<ProposalDecisionDto>(requested.Length);
        foreach (var id in requested)
        {
            if (!byId.TryGetValue(id, out var proposal))
            {
                results.Add(new ProposalDecisionDto { Id = id, Error = "ERR_NOT_FOUND" });
                continue;
            }

            if (approve && proposal.Text.Length > 0)
            {
                try
                {
                    Placeholders.RequireSame(proposal.Key.SourceValue, proposal.Text);
                }
                catch (BadRequestException exception)
                {
                    results.Add(new ProposalDecisionDto { Id = id, Error = exception.Message, Detail = exception.Detail });
                    continue;
                }
            }

            if (approve)
            {
                ApplyValue(proposal.Key, proposal.Locale, proposal.Text);
                db.ProjectProposals.Remove(proposal);
            }
            else
            {
                proposal.Status = ProjectProposal.Refused;
                proposal.UpdatedAt = DateTime.UtcNow;
            }

            results.Add(new ProposalDecisionDto { Id = id });
        }

        await db.SaveChangesAsync(token);
        return new ProposalBatchDto
        {
            Results = results.ToArray(),
            Pending = (await LoadPendingAsync(stored.Id, code, token)).ToArray(),
        };
    }

    private async Task<string?> VisibleCodeAsync(Project project, Guid accountId, string? locale, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return null;
        }

        return await RequireVisibleLocaleAsync(project, accountId, locale, token);
    }

    private async Task<string> RequireVisibleLocaleAsync(Project project, Guid accountId, string? locale, CancellationToken token)
    {
        var code = NormalizeLocale(locale);
        var visible = await VisibleLocalesAsync(project, accountId, token);
        if (!visible.Contains(code))
        {
            throw new ForbiddenException("ERR_PROJECT_FORBIDDEN");
        }

        return code;
    }

    private async Task<ProjectFile> RequireFileAsync(Guid projectId, string? file, CancellationToken token)
    {
        var name = file?.Trim() ?? "";
        var stored = await db.ProjectFiles.FirstOrDefaultAsync(
            item => item.ProjectId == projectId && item.Name == name,
            token);
        return stored ?? throw new NotFoundException("ERR_NOT_FOUND");
    }

    private async Task<IReadOnlyList<TranslationEntryDto>> LoadEntriesAsync(Guid fileId, string locale, CancellationToken token)
    {
        var keys = await db.ProjectKeys
            .AsNoTracking()
            .Where(key => key.FileId == fileId)
            .OrderBy(key => key.Ordinal)
            .ToListAsync(token);
        var keyIds = keys.Select(key => key.Id).ToArray();
        var values = await db.ProjectValues
            .AsNoTracking()
            .Where(value => value.Locale == locale && keyIds.Contains(value.KeyId))
            .ToListAsync(token);
        var proposals = await db.ProjectProposals
            .AsNoTracking()
            .Include(proposal => proposal.Author)
            .Where(proposal => proposal.Locale == locale && keyIds.Contains(proposal.KeyId))
            .ToListAsync(token);
        var valueByKey = values.ToDictionary(value => value.KeyId);
        var proposalByKey = proposals.ToDictionary(proposal => proposal.KeyId);
        return keys
            .Select(key => ToEntry(
                key,
                valueByKey.GetValueOrDefault(key.Id),
                proposalByKey.GetValueOrDefault(key.Id)))
            .ToArray();
    }

    private async Task<IReadOnlyList<ProposalDto>> LoadPendingAsync(Guid projectId, string? locale, CancellationToken token)
    {
        var query = db.ProjectProposals
            .AsNoTracking()
            .Include(proposal => proposal.Author)
            .Include(proposal => proposal.Key)
            .ThenInclude(key => key.File)
            .Include(proposal => proposal.Key)
            .ThenInclude(key => key.Values)
            .Where(proposal => proposal.Status == ProjectProposal.Pending
                && proposal.Key.File.ProjectId == projectId);
        if (!string.IsNullOrEmpty(locale))
        {
            query = query.Where(proposal => proposal.Locale == locale);
        }

        var rows = await query.ToListAsync(token);
        return rows
            .OrderBy(proposal => proposal.Locale, StringComparer.Ordinal)
            .ThenBy(proposal => proposal.Key.File.Name, StringComparer.Ordinal)
            .ThenBy(proposal => proposal.Key.Ordinal)
            .Select(proposal => new ProposalDto
            {
                Id = proposal.Id,
                KeyId = proposal.KeyId,
                Locale = proposal.Locale,
                FileName = proposal.Key.File.Name,
                Path = proposal.Key.Path,
                SourceValue = proposal.Key.SourceValue,
                CurrentText = proposal.Key.Values.FirstOrDefault(value => value.Locale == proposal.Locale)?.Text ?? "",
                Text = proposal.Text,
                AuthorName = proposal.Author.Name,
            })
            .ToArray();
    }

    private static void ApplyValue(ProjectKey key, string locale, string text)
    {
        var value = key.Values.FirstOrDefault(item => item.Locale == locale);
        if (value is null)
        {
            value = new ProjectValue { Locale = locale };
            key.Values.Add(value);
        }

        value.Text = text;
        value.SourceSnapshot = key.SourceValue;
        value.UpdatedAt = DateTime.UtcNow;
    }

    private static TranslationEntryDto ToEntry(ProjectKey key, ProjectValue? value, ProjectProposal? proposal)
    {
        var text = value?.Text ?? "";
        return new TranslationEntryDto
        {
            KeyId = key.Id,
            Path = key.Path,
            SourceValue = key.SourceValue,
            Text = text,
            Status = Placeholders.Status(key.SourceValue, text, value?.SourceSnapshot ?? ""),
            ProposalId = proposal?.Id,
            ProposalText = proposal?.Text,
            ProposalStatus = proposal?.Status,
            ProposalAuthor = proposal?.Author?.Name,
        };
    }
}
