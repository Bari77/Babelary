namespace Core.Accounts;

/// <summary>
/// CLI token as listed in the profile. The secret is not included.
/// </summary>
public class TokenDto
{
    /// <summary>Identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Name chosen at creation.</summary>
    public string Name { get; set; } = "";

    /// <summary>Creation instant, UTC.</summary>
    public DateTime CreatedAt { get; set; }
}
