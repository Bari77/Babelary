using Core.Accounts;

namespace Core.Projects;

/// <summary>
/// A translation suggested by a member. It replaces the saved value only after the owner approves it.
/// </summary>
public class ProjectProposal
{
    /// <summary>Pending, waiting for the owner.</summary>
    public const string Pending = "pending";

    /// <summary>Refused. The saved value is unchanged.</summary>
    public const string Refused = "refused";

    /// <summary>Identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Key this proposal translates.</summary>
    public Guid KeyId { get; set; }

    /// <summary>Target locale, <c>xx-XX</c>.</summary>
    public string Locale { get; set; } = "";

    /// <summary>Suggested text. Empty means a proposal to clear the translation.</summary>
    public string Text { get; set; } = "";

    /// <summary>Account that sent the suggestion.</summary>
    public Guid AuthorAccountId { get; set; }

    /// <summary><see cref="Pending"/> or <see cref="Refused"/>.</summary>
    public string Status { get; set; } = Pending;

    /// <summary>Last change instant, UTC.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Key.</summary>
    public ProjectKey Key { get; set; } = null!;

    /// <summary>Author.</summary>
    public Account Author { get; set; } = null!;
}
