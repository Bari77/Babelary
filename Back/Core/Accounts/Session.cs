namespace Core.Accounts;

/// <summary>
/// Server-side web session. Logout deletes the row.
/// </summary>
public class Session
{
    /// <summary>Identifier stored in the cookie ticket.</summary>
    public Guid Id { get; set; }

    /// <summary>Account this session belongs to.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Account navigation.</summary>
    public Account? Account { get; set; }

    /// <summary>Creation instant, UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Moment after which the session is refused. Sliding, 30 days.</summary>
    public DateTime ExpiresAt { get; set; }
}
