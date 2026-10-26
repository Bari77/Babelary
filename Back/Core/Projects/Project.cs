using Core.Accounts;

namespace Core.Projects;

/// <summary>
/// One application whose texts are translated. The slug is the stable identifier.
/// </summary>
public class Project
{
    /// <summary>Identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Account that created the project.</summary>
    public Guid OwnerAccountId { get; set; }

    /// <summary>Stable identifier, such as <c>kert-front</c>. It never changes.</summary>
    public string Slug { get; set; } = "";

    /// <summary>Display name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Source locale, <c>xx-XX</c>. Locked once a key exists.</summary>
    public string SourceLocale { get; set; } = "";

    /// <summary>Creation instant, UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last change instant, UTC.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Owner account.</summary>
    public Account Owner { get; set; } = null!;

    /// <summary>Target locales.</summary>
    public List<ProjectTarget> Targets { get; set; } = [];

    /// <summary>Source files.</summary>
    public List<ProjectFile> Files { get; set; } = [];

    /// <summary>Translators.</summary>
    public List<ProjectMember> Members { get; set; } = [];

    /// <summary>Invitations, including those already used or cancelled.</summary>
    public List<ProjectInvite> Invites { get; set; } = [];
}
