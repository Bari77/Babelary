namespace Core.Projects;

/// <summary>
/// One target locale carried by an invitation.
/// </summary>
public class ProjectInviteLocale
{
    /// <summary>Identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Invitation.</summary>
    public Guid InviteId { get; set; }

    /// <summary>Target locale, <c>xx-XX</c>.</summary>
    public string Locale { get; set; } = "";

    /// <summary>Invitation.</summary>
    public ProjectInvite Invite { get; set; } = null!;
}
