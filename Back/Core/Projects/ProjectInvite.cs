namespace Core.Projects;

/// <summary>
/// A single-use invitation. The secret is stored only as a hash. Nothing is e-mailed.
/// </summary>
public class ProjectInvite
{
    /// <summary>Identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Project.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Invited login, lowercase.</summary>
    public string Email { get; set; } = "";

    /// <summary>SHA-256 of the shared code, hex.</summary>
    public string TokenHash { get; set; } = "";

    /// <summary>Expiry, UTC. Seven days after creation.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Creation instant, UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Set when the link has been used.</summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>Set when the owner cancels the link.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Project.</summary>
    public Project Project { get; set; } = null!;

    /// <summary>Target locales offered by this link.</summary>
    public List<ProjectInviteLocale> Locales { get; set; } = [];
}
