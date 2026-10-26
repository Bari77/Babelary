namespace Core.Projects;

/// <summary>
/// Project visible in the list and on its page.
/// </summary>
public class ProjectDto
{
    /// <summary>Stable identifier.</summary>
    public string Slug { get; set; } = "";

    /// <summary>Display name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Source locale, <c>xx-XX</c>.</summary>
    public string SourceLocale { get; set; } = "";

    /// <summary>Target locale codes, ordered by code.</summary>
    public string[] Targets { get; set; } = [];

    /// <summary>Progress of each target locale, in the same order as <see cref="Targets"/>.</summary>
    public LocaleProgressDto[] Progress { get; set; } = [];

    /// <summary>Number of target locales.</summary>
    public int TargetCount { get; set; }

    /// <summary>Translated values across target locales.</summary>
    public int Translated { get; set; }

    /// <summary>Proposals still waiting for the owner.</summary>
    public int Pending { get; set; }

    /// <summary>Waiting proposals whose key has no saved text.</summary>
    public int PendingOpen { get; set; }

    /// <summary>Values expected across target locales.</summary>
    public int Total { get; set; }

    /// <summary>True once a key exists and the source locale can no longer change.</summary>
    public bool SourceLocaleLocked { get; set; }
}
