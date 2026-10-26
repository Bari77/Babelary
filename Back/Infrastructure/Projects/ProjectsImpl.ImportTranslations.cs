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
        CancellationToken token)
    {
        var stored = await RequireOwnedAsync(accountId, project, token);
        var code = await RequireVisibleLocaleAsync(stored, accountId, locale, token);
        if (files is null || files.Count == 0)
        {
            throw new BadRequestException("ERR_FILE_REQUIRED");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var results = new List<ImportTranslationFileDto>(files.Count);
        var updated = 0;
        foreach (var file in files)
        {
            var result = await ImportTranslationFileAsync(stored.Id, code, file, seen, token);
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
            leaves = SourceDocument.ReadTranslations(file?.Content);
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

            if (text.Length == 0)
            {
                skipped++;
                continue;
            }

            try
            {
                Placeholders.RequireSame(key.SourceValue, text);
            }
            catch (BadRequestException exception)
            {
                issues.Add(new ImportKeyIssueDto { Path = path, Error = exception.Message, Detail = exception.Detail });
                continue;
            }

            var value = key.Values.FirstOrDefault(item => item.Locale == locale);
            if (value is not null && string.Equals(value.Text, text, StringComparison.Ordinal))
            {
                unchanged++;
                continue;
            }

            ApplyValue(key, locale, text);
            if (proposalByKey.Remove(key.Id, out var proposal))
            {
                db.ProjectProposals.Remove(proposal);
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
}
