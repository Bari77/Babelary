using Core.Projects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Projects;

public partial class ProjectsImpl
{
    /// <inheritdoc />
    public async Task<SyncStatusDto> StatusAsync(Guid accountId, string? project, CancellationToken token)
    {
        var stored = await RequireVisibleAsync(accountId, project, token);
        var visible = await VisibleLocalesAsync(stored, accountId, token);
        var codes = visible.ToArray();
        var keys = await db.ProjectKeys
            .AsNoTracking()
            .Where(key => key.File.ProjectId == stored.Id)
            .OrderBy(key => key.File.Name)
            .ThenBy(key => key.Ordinal)
            .Select(key => new { key.Id, File = key.File.Name, key.Path, key.SourceValue })
            .ToListAsync(token);
        var values = await db.ProjectValues
            .AsNoTracking()
            .Where(value => codes.Contains(value.Locale) && value.Key.File.ProjectId == stored.Id)
            .Select(value => new { value.KeyId, value.Locale, value.Text, value.SourceSnapshot })
            .ToListAsync(token);
        var byKey = values
            .GroupBy(value => value.KeyId)
            .ToDictionary(group => group.Key, group => group.ToDictionary(value => value.Locale, StringComparer.Ordinal));

        var locales = stored.Targets
            .Select(target => target.Locale)
            .Where(visible.Contains)
            .OrderBy(locale => locale, StringComparer.Ordinal)
            .Select(locale =>
            {
                var translated = 0;
                var review = 0;
                var empty = new List<SyncEmptyKeyDto>();
                foreach (var key in keys)
                {
                    byKey.TryGetValue(key.Id, out var rows);
                    var text = "";
                    var snapshot = "";
                    if (rows is not null && rows.TryGetValue(locale, out var value))
                    {
                        text = value.Text;
                        snapshot = value.SourceSnapshot;
                    }

                    var status = Placeholders.Status(key.SourceValue, text, snapshot);
                    if (status == "review")
                    {
                        review++;
                    }
                    else if (status == "empty")
                    {
                        empty.Add(new SyncEmptyKeyDto { File = key.File, Path = key.Path });
                    }
                    else
                    {
                        translated++;
                    }
                }

                return new SyncLocaleDto
                {
                    Locale = locale,
                    Total = keys.Count,
                    Translated = translated,
                    Review = review,
                    Empty = empty.ToArray(),
                };
            })
            .ToArray();

        return new SyncStatusDto { Locales = locales };
    }
}
