namespace Core.Accounts;

/// <summary>
/// Password constraints for v1.
/// </summary>
public static class PasswordRules
{
    /// <summary>Minimum accepted length.</summary>
    public const int MinimumLength = 12;

    /// <summary>
    /// A password is accepted when it has at least <see cref="MinimumLength"/> characters.
    /// </summary>
    public static bool IsValid(string? password) => password is { Length: >= MinimumLength };
}
