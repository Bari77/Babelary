using Core.Accounts;

namespace Core.Projects;

/// <summary>
/// A translator invited onto a project. The account stays if the membership is removed.
/// </summary>
public class ProjectMember
{
    /// <summary>Identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Project.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Account.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Creation instant, UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Project.</summary>
    public Project Project { get; set; } = null!;

    /// <summary>Account.</summary>
    public Account Account { get; set; } = null!;

    /// <summary>Target locales this member may translate.</summary>
    public List<ProjectMemberLocale> Locales { get; set; } = [];
}
