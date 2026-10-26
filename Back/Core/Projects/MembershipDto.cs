namespace Core.Projects;

/// <summary>A member of a project.</summary>
public class ProjectMemberDto
{
    /// <summary>Account identifier.</summary>
    public Guid AccountId { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Login.</summary>
    public string Email { get; set; } = "";

    /// <summary>Assigned target locales.</summary>
    public string[] Locales { get; set; } = [];
}

/// <summary>An invitation that can still be opened.</summary>
public class ProjectInviteDto
{
    /// <summary>Identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Invited login.</summary>
    public string Email { get; set; } = "";

    /// <summary>Assigned target locales.</summary>
    public string[] Locales { get; set; } = [];

    /// <summary>Expiry, UTC.</summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>Invitation just created. <see cref="Code"/> is shown once.</summary>
public class CreatedInviteDto : ProjectInviteDto
{
    /// <summary>Secret to put in the shared link. It is not stored.</summary>
    public string Code { get; set; } = "";
}

/// <summary>What the invitation link shows before a password is chosen.</summary>
public class InvitePreviewDto
{
    /// <summary>Project display name.</summary>
    public string ProjectName { get; set; } = "";

    /// <summary>Stable project identifier.</summary>
    public string ProjectSlug { get; set; } = "";

    /// <summary>Invited login.</summary>
    public string Email { get; set; } = "";

    /// <summary>Target locales the member will receive.</summary>
    public string[] Locales { get; set; } = [];

    /// <summary>True when this e-mail already has an account and must sign in.</summary>
    public bool AccountExists { get; set; }
}

/// <summary>Progress of one source file in one target locale.</summary>
public class FileProgressDto
{
    /// <summary>File name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Translated, identical, or to-review values.</summary>
    public int Translated { get; set; }

    /// <summary>Keys in the file.</summary>
    public int Total { get; set; }

    /// <summary>Proposals still waiting for the owner.</summary>
    public int Pending { get; set; }

    /// <summary>Waiting proposals whose key has no saved text in this locale.</summary>
    public int PendingOpen { get; set; }
}
