namespace Core.Projects;

/// <summary>
/// One target locale assigned to a member.
/// </summary>
public class ProjectMemberLocale
{
    /// <summary>Identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Membership.</summary>
    public Guid MemberId { get; set; }

    /// <summary>Target locale, <c>xx-XX</c>.</summary>
    public string Locale { get; set; } = "";

    /// <summary>Membership.</summary>
    public ProjectMember Member { get; set; } = null!;
}
