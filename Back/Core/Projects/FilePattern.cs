using System.Text;
using System.Text.RegularExpressions;
using Core.Exceptions;

namespace Core.Projects;

/// <summary>
/// Relative path of a translation file. <c>{locale}</c> is the language being read or written,
/// <c>{source}</c> is the project source language, <c>{name}</c> is the file stem, and <c>{file}</c> is the file name.
/// <c>*</c> is a <c>{file}</c> segment, and <c>*.json</c> is <c>{name}.json</c>.
/// </summary>
public static partial class FilePattern
{
    /// <summary>One JSON file per name, inside a folder named after the locale.</summary>
    public const string Folder = "{locale}/{name}.json";

    /// <summary>One JSON file whose name is the locale.</summary>
    public const string LocaleFile = "{locale}.json";

    private const int MaxLength = 200;

    /// <summary>Accepts a pattern or returns <see cref="Folder"/> when it is empty.</summary>
    public static string Normalize(string? pattern)
    {
        var value = (pattern ?? "").Trim().Replace('\\', '/');
        if (value.Length == 0)
        {
            return Folder;
        }

        if (value.Length > MaxLength || value.StartsWith('/') || value.EndsWith('/') || value.Contains("..", StringComparison.Ordinal))
        {
            throw new BadRequestException("ERR_FILE_PATTERN_INVALID");
        }

        var segments = value.Split('/');
        if (segments.Any(segment => segment.Length == 0))
        {
            throw new BadRequestException("ERR_FILE_PATTERN_INVALID");
        }

        for (var index = 0; index < segments.Length; index++)
        {
            segments[index] = segments[index] switch
            {
                "*" => "{file}",
                "*.json" => "{name}.json",
                _ => segments[index],
            };
        }

        value = string.Join('/', segments);
        if (segments.Any(segment => !SegmentPattern().IsMatch(segment))
            || Count(value, "{locale}") != 1
            || Count(value, "{name}") + Count(value, "{file}") > 1
            || (!value.EndsWith(".json", StringComparison.Ordinal) && !value.Contains("{file}", StringComparison.Ordinal)))
        {
            throw new BadRequestException("ERR_FILE_PATTERN_INVALID");
        }

        return value;
    }

    /// <summary>Stored file name for a path that matches <paramref name="pattern"/> in <paramref name="locale"/>.</summary>
    public static bool TryMatch(string pattern, string locale, string sourceLocale, string relativePath, out string storedName)
    {
        storedName = "";
        var normalized = Normalize(pattern);
        var match = Matcher(normalized, locale, sourceLocale).Match(relativePath.Replace('\\', '/'));
        if (!match.Success)
        {
            return false;
        }

        if (match.Groups["name"].Success)
        {
            storedName = match.Groups["name"].Value + ".json";
            return true;
        }

        if (match.Groups["file"].Success)
        {
            storedName = match.Groups["file"].Value;
            return true;
        }

        storedName = Path.GetFileName(Render(normalized, sourceLocale, sourceLocale, ""));
        return storedName.Length > 0;
    }

    /// <summary>Path of <paramref name="storedName"/> inside the package of <paramref name="locale"/>.</summary>
    public static string ExportPath(string pattern, string locale, string sourceLocale, string storedName)
    {
        var normalized = Normalize(pattern);
        if (Count(normalized, "{name}") + Count(normalized, "{file}") == 0)
        {
            var sourcePath = Render(normalized, sourceLocale, sourceLocale, storedName);
            if (!string.Equals(Path.GetFileName(sourcePath), storedName, StringComparison.Ordinal))
            {
                return storedName;
            }
        }

        return Render(normalized, locale, sourceLocale, storedName);
    }

    private static string Render(string pattern, string locale, string sourceLocale, string storedName)
    {
        var stem = storedName.EndsWith(".json", StringComparison.Ordinal) ? storedName[..^5] : storedName;
        return pattern
            .Replace("{locale}", locale, StringComparison.Ordinal)
            .Replace("{source}", sourceLocale, StringComparison.Ordinal)
            .Replace("{name}", stem, StringComparison.Ordinal)
            .Replace("{file}", storedName, StringComparison.Ordinal);
    }

    private static Regex Matcher(string pattern, string locale, string sourceLocale)
    {
        var expression = new StringBuilder("^");
        for (var index = 0; index < pattern.Length;)
        {
            if (Consume(pattern, ref index, "{locale}"))
            {
                expression.Append(Regex.Escape(locale));
            }
            else if (Consume(pattern, ref index, "{source}"))
            {
                expression.Append(Regex.Escape(sourceLocale));
            }
            else if (Consume(pattern, ref index, "{name}"))
            {
                expression.Append("(?<name>[A-Za-z0-9][A-Za-z0-9._-]*)");
            }
            else if (Consume(pattern, ref index, "{file}"))
            {
                expression.Append("(?<file>[A-Za-z0-9][A-Za-z0-9._-]*\\.json)");
            }
            else
            {
                expression.Append(Regex.Escape(pattern[index].ToString()));
                index++;
            }
        }

        expression.Append('$');
        return new Regex(expression.ToString(), RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
    }

    private static bool Consume(string pattern, ref int index, string token)
    {
        if (!pattern.AsSpan(index).StartsWith(token))
        {
            return false;
        }

        index += token.Length;
        return true;
    }

    private static int Count(string value, string token)
        => value.Split(token).Length - 1;

    [GeneratedRegex("^(?:[A-Za-z0-9._-]|\\{(?:locale|source|name|file)\\})+$", RegexOptions.CultureInvariant)]
    private static partial Regex SegmentPattern();
}
