namespace Core.Accounts;

/// <summary>
/// Personal CLI token. Only the hash is stored.
/// </summary>
public class CliToken
{
    /// <summary>Identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Account that created the token.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Account navigation.</summary>
    public Account? Account { get; set; }

    /// <summary>Name shown in the profile.</summary>
    public string Name { get; set; } = "";

    /// <summary>SHA-256 hex of the secret.</summary>
    public string Hash { get; set; } = "";

    /// <summary>Creation instant, UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Revocation instant, UTC. Null while the token is usable.</summary>
    public DateTime? RevokedAt { get; set; }
}
