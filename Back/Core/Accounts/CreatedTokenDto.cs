namespace Core.Accounts;

/// <summary>
/// CLI token just created. <see cref="Secret"/> is returned only this once.
/// </summary>
public class CreatedTokenDto
{
    /// <summary>Identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Name chosen at creation.</summary>
    public string Name { get; set; } = "";

    /// <summary>Plain secret. It cannot be read again.</summary>
    public string Secret { get; set; } = "";

    /// <summary>Creation instant, UTC.</summary>
    public DateTime CreatedAt { get; set; }
}
