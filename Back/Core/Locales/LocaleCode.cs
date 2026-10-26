using System.Text.RegularExpressions;

namespace Core.Locales;

/// <summary>
/// Locale code <c>xx-XX</c>, such as <c>en-US</c>.
/// </summary>
public static partial class LocaleCode
{
    [GeneratedRegex("^[a-z]{2}-[A-Z]{2}$")]
    private static partial Regex Pattern();

    /// <summary>
    /// Returns true when <paramref name="code"/> matches <c>xx-XX</c>.
    /// </summary>
    public static bool IsValid(string? code) => code is not null && Pattern().IsMatch(code);
}
