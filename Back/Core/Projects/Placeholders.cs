using System.Text.RegularExpressions;
using Core.Exceptions;

namespace Core.Projects;

/// <summary>
/// <c>{{name}}</c> and Angular <c>{$name}</c> tokens shared by a source string and its translation.
/// </summary>
public static partial class Placeholders
{
    [GeneratedRegex(@"\{\{[A-Za-z0-9_.]+\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex MustachePattern();

    [GeneratedRegex(@"\{\$[A-Za-z0-9_]+\}", RegexOptions.CultureInvariant)]
    private static partial Regex AngularPattern();

    /// <summary>
    /// Refuses a non-empty translation whose tokens differ in name or count from the source.
    /// </summary>
    public static void RequireSame(string source, string translation)
    {
        var expected = Count(source);
        var actual = Count(translation);
        foreach (var (name, count) in expected)
        {
            actual.TryGetValue(name, out var found);
            if (found != count)
            {
                throw new BadRequestException("ERR_PLACEHOLDER_MISSING", name);
            }
        }

        foreach (var name in actual.Keys)
        {
            if (!expected.ContainsKey(name))
            {
                throw new BadRequestException("ERR_PLACEHOLDER_UNKNOWN", name);
            }
        }
    }

    /// <summary>Status of one saved target value.</summary>
    public static string Status(string source, string text, string snapshot)
    {
        if (text.Length == 0)
        {
            return "empty";
        }

        if (string.Equals(text, source, StringComparison.Ordinal))
        {
            return "identical";
        }

        if (!string.Equals(snapshot, source, StringComparison.Ordinal))
        {
            return "review";
        }

        return "translated";
    }

    private static Dictionary<string, int> Count(string text)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        Count(MustachePattern(), text, counts);
        Count(AngularPattern(), text, counts);
        return counts;
    }

    private static void Count(Regex pattern, string text, Dictionary<string, int> counts)
    {
        foreach (Match match in pattern.Matches(text))
        {
            var token = match.Value;
            counts[token] = counts.GetValueOrDefault(token) + 1;
        }
    }
}
