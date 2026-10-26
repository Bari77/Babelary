using System.Globalization;

namespace Core.Locales;

/// <summary>
/// Locales the product accepts: specific cultures whose code is <c>xx-XX</c>.
/// </summary>
public static class LocaleCatalog
{
    private static readonly HashSet<string> Known = new(
        CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(culture => culture.Name)
            .Where(LocaleCode.IsValid),
        StringComparer.Ordinal);

    /// <summary>
    /// Every accepted locale code, ordered by code.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = Known.OrderBy(code => code, StringComparer.Ordinal).ToArray();

    /// <summary>
    /// Returns true when <paramref name="code"/> is a specific culture in <c>xx-XX</c> form.
    /// </summary>
    public static bool Contains(string? code) => code is not null && Known.Contains(code);
}
