namespace Core.Accounts;

/// <summary>
/// A person who can sign in. The first account owns the instance.
/// </summary>
public class Account
{
    /// <summary>Identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Unique login, stored lowercase.</summary>
    public string Email { get; set; } = "";

    /// <summary>PBKDF2 hash. The password itself is never stored.</summary>
    public string PasswordHash { get; set; } = "";

    /// <summary>True for the account created at installation.</summary>
    public bool IsInstanceOwner { get; set; }

    /// <summary>Creation instant, UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Web sessions.</summary>
    public List<Session> Sessions { get; set; } = [];

    /// <summary>CLI tokens.</summary>
    public List<CliToken> Tokens { get; set; } = [];
}
