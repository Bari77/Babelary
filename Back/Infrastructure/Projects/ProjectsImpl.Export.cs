using System.IO.Compression;
using System.Text;
using Core.Projects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Projects;

public partial class ProjectsImpl
{
    /// <inheritdoc />
    public async Task<LocaleExport> ExportLocaleAsync(Guid accountId, string? project, string? locale, CancellationToken token)
    {
        var stored = await RequireVisibleAsync(accountId, project, token);
        var requested = locale?.Trim() ?? "";
        var sourceExport = string.Equals(requested, stored.SourceLocale, StringComparison.Ordinal);
        var code = sourceExport
            ? stored.SourceLocale
            : await RequireVisibleLocaleAsync(stored, accountId, locale, token);
        var files = await db.ProjectFiles
            .AsNoTracking()
            .Where(file => file.ProjectId == stored.Id)
            .OrderBy(file => file.Name)
            .Select(file => new { file.Id, file.Name, file.Catalog })
            .ToListAsync(token);
        var keys = await db.ProjectKeys
            .AsNoTracking()
            .Where(key => key.File.ProjectId == stored.Id)
            .OrderBy(key => key.Ordinal)
            .Select(key => new { key.Id, key.FileId, key.Path, key.SourceValue })
            .ToListAsync(token);
        var textByKey = sourceExport
            ? keys.ToDictionary(key => key.Id, key => key.SourceValue)
            : await db.ProjectValues
                .AsNoTracking()
                .Where(value => value.Locale == code && value.Key.File.ProjectId == stored.Id)
                .ToDictionaryAsync(value => value.KeyId, value => value.Text, token);

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var leaves = keys
                    .Where(key => key.FileId == file.Id)
                    .Select(key => new KeyValuePair<string, string>(key.Path, textByKey.GetValueOrDefault(key.Id) ?? ""));
                var entry = archive.CreateEntry(file.Name, CompressionLevel.Optimal);
                await using var entryStream = entry.Open();
                await using var writer = new StreamWriter(entryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                await writer.WriteAsync(file.Catalog == ProjectFile.AngularCatalog
                    ? SourceDocument.Write(leaves, code)
                    : SourceDocument.Write(leaves));
            }
        }

        return new LocaleExport
        {
            FileName = code + ".zip",
            Content = stream.ToArray(),
        };
    }
}
