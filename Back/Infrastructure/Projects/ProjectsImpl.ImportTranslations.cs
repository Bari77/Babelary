using Core.Exceptions;
using Core.Projects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Projects;

public partial class ProjectsImpl
{
    /// <inheritdoc />
    public async Task<ImportTranslationsDto> ImportTranslationsAsync(
        Guid accountId,
        string? project,
        string? locale,
        IReadOnlyList<SourceFileUpload>? files,
        CancellationToken token,
        bool overwriteEmpty = false)
    {
        var stored = await RequireVisibleAsync(accountId, project, token);
        var code = await RequireVisibleLocaleAsync(stored, accountId, locale, token);
        if (files is null || files.Count == 0)
        {
            throw new BadRequestException("ERR_FILE_REQUIRED");
        }

        var owner = stored.OwnerAccountId == accountId;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var results = new List<ImportTranslationFileDto>(files.Count);
        var updated = 0;
        foreach (var file in files)
        {
            var result = await ImportTranslationFileAsync(stored.Id, code, file, seen, owner, accountId, overwriteEmpty, token);
            updated += result.Updated;
            results.Add(result);
        }

        if (updated > 0)
        {
            stored.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(token);
        return new ImportTranslationsDto
        {
            Files = results.ToArray(),
            Project = await CompleteAsync(stored, accountId, token),
        };
    }

    private async Task<ImportTranslationFileDto> ImportTranslationFileAsync(
        Guid projectId,
        string locale,
        SourceFileUpload? file,
        HashSet<string> seen,
        bool owner,
        Guid accountId,
        bool overwriteEmpty,
        CancellationToken token)
    {
        var name = file?.Name?.Trim() ?? "";
        if (name.Length == 0 || name.Length > 80 || name.Contains("..", StringComparison.Ordinal) || !FileNamePattern().IsMatch(name))
        {
            return new ImportTranslationFileDto { Name = name, Error = "ERR_FILE_NAME_INVALID" };
        }

        if (!seen.Add(name))
        {
            return new ImportTranslationFileDto { Name = name, Error = "ERR_FILE_NAME_INVALID" };
        }

        Dictionary<string, string> leaves;
        try
        {
            leaves = SourceDocument.Parse(file?.Content, allowFlatKeys: true).Leaves;
        }
        catch (BadRequestException exception)
        {
            return new ImportTranslationFileDto { Name = name, Error = exception.Message };
        }

        var existing = await db.ProjectFiles
            .Include(item => item.Keys)
            .ThenInclude(key => key.Values)
            .FirstOrDefaultAsync(item => item.ProjectId == projectId && item.Name == name, token);
        if (existing is null)
        {
            return new ImportTranslationFileDto { Name = name, Error = "ERR_FILE_MISSING" };
        }

        var proposals = await db.ProjectProposals
            .Where(proposal => proposal.Locale == locale && proposal.Key.FileId == existing.Id)
            .ToListAsync(token);
        var proposalByKey = proposals.ToDictionary(proposal => proposal.KeyId);
        var byPath = existing.Keys.ToDictionary(key => key.Path, StringComparer.Ordinal);
        var updated = 0;
        var unchanged = 0;
        var skipped = 0;
        var ignored = 0;
        var present = new HashSet<string>(StringComparer.Ordinal);
        var issues = new List<ImportKeyIssueDto>();
        foreach (var (path, text) in leaves)
        {
            if (!byPath.TryGetValue(path, out var key))
            {
                ignored++;
                continue;
            }

            present.Add(path);

            if (text.Length == 0 && !overwriteEmpty)
            {
                skipped++;
                continue;
            }

            if (text.Length > 0)
            {
                try
                {
                    Placeholders.RequireSame(key.SourceValue, text);
                }
                catch (BadRequestException exception)
                {
                    issues.Add(new ImportKeyIssueDto { Path = path, Error = exception.Message, Detail = exception.Detail });
                    continue;
                }
            }

            if (owner)
            {
                var value = key.Values.FirstOrDefault(item => item.Locale == locale);
                var same = value is not null && string.Equals(value.Text, text, StringComparison.Ordinal);
                if (same && !proposalByKey.ContainsKey(key.Id))
                {
                    unchanged++;
                    continue;
                }

                ApplyValue(key, locale, text);
                if (proposalByKey.Remove(key.Id, out var proposal))
                {
                    db.ProjectProposals.Remove(proposal);
                }
            }
            else if (!Propose(key.Id, locale, accountId, text, proposalByKey))
            {
                unchanged++;
                continue;
            }

            updated++;
        }

        return new ImportTranslationFileDto
        {
            Name = name,
            Updated = updated,
            Unchanged = unchanged,
            Skipped = skipped,
            Ignored = ignored,
            Missing = byPath.Keys.Count(path => !present.Contains(path)),
            Issues = issues.ToArray(),
        };
    }

    /// <summary>Stores a member proposal. Returns false when the pending text is already the same.</summary>
    private bool Propose(
        Guid keyId,
        string locale,
        Guid accountId,
        string text,
        Dictionary<Guid, ProjectProposal> proposalByKey)
    {
        if (!proposalByKey.TryGetValue(keyId, out var proposal))
        {
            proposal = new ProjectProposal
            {
                KeyId = keyId,
                Locale = locale,
                AuthorAccountId = accountId,
            };
            db.ProjectProposals.Add(proposal);
            proposalByKey[keyId] = proposal;
        }
        else if (string.Equals(proposal.Text, text, StringComparison.Ordinal)
            && proposal.Status == ProjectProposal.Pending
            && proposal.AuthorAccountId == accountId)
        {
            return false;
        }

        proposal.Text = text;
        proposal.Status = ProjectProposal.Pending;
        proposal.AuthorAccountId = accountId;
        proposal.UpdatedAt = DateTime.UtcNow;
        return true;
    }
}
