namespace Core.Projects;

/// <summary>
/// A target locale of a project. Values for this locale are created with the keys.
/// </summary>
public class ProjectTarget
{
    /// <summary>Identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Project that owns this locale.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Locale code, <c>xx-XX</c>. Never the source locale.</summary>
    public string Locale { get; set; } = "";

    /// <summary>Creation instant, UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Project.</summary>
    public Project Project { get; set; } = null!;
}
