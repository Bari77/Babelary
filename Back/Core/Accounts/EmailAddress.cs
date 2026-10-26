using System.Text.RegularExpressions;

namespace Core.Accounts;

/// <summary>
/// Normalizes the login email.
/// </summary>
public static partial class EmailAddress
{
    /// <summary>
    /// Trims and lowercases a usable email, or returns null.
    /// </summary>
    public static string? Normalize(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length > 320 || !Address().IsMatch(normalized))
        {
            return null;
        }

        return normalized;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex Address();
}
