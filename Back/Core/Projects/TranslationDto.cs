namespace Core.Projects;

/// <summary>One key in the editor, with the saved value and any proposal.</summary>
public class TranslationEntryDto
{
    /// <summary>Key identifier.</summary>
    public Guid KeyId { get; set; }

    /// <summary>Source file name, such as <c>account.json</c>.</summary>
    public string FileName { get; set; } = "";

    /// <summary>Dotted path.</summary>
    public string Path { get; set; } = "";

    /// <summary>Source text.</summary>
    public string SourceValue { get; set; } = "";

    /// <summary>Approved translation. Empty means still to translate.</summary>
    public string Text { get; set; } = "";

    /// <summary><c>empty</c>, <c>translated</c>, <c>identical</c>, or <c>review</c>.</summary>
    public string Status { get; set; } = "empty";

    /// <summary>Proposal identifier, when one exists.</summary>
    public Guid? ProposalId { get; set; }

    /// <summary>Suggested text.</summary>
    public string? ProposalText { get; set; }

    /// <summary><c>pending</c> or <c>refused</c>.</summary>
    public string? ProposalStatus { get; set; }

    /// <summary>Name of the person who suggested the text.</summary>
    public string? ProposalAuthor { get; set; }
}

/// <summary>A pending proposal shown in the review list.</summary>
public class ProposalDto
{
    /// <summary>Proposal identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Key identifier.</summary>
    public Guid KeyId { get; set; }

    /// <summary>Target locale, <c>xx-XX</c>.</summary>
    public string Locale { get; set; } = "";

    /// <summary>Source file name.</summary>
    public string FileName { get; set; } = "";

    /// <summary>Dotted path.</summary>
    public string Path { get; set; } = "";

    /// <summary>Source text.</summary>
    public string SourceValue { get; set; } = "";

    /// <summary>Approved translation.</summary>
    public string CurrentText { get; set; } = "";

    /// <summary>Suggested text.</summary>
    public string Text { get; set; } = "";

    /// <summary>Author display name.</summary>
    public string AuthorName { get; set; } = "";
}

/// <summary>What happened to one proposal in a bulk decision.</summary>
public class ProposalDecisionDto
{
    /// <summary>Proposal identifier.</summary>
    public Guid Id { get; set; }

    /// <summary><c>ERR_*</c> when this row was left unchanged.</summary>
    public string? Error { get; set; }

    /// <summary>Token named by a placeholder error.</summary>
    public string? Detail { get; set; }
}

/// <summary>Bulk approval or refusal.</summary>
public class ProposalBatchDto
{
    /// <summary>One result per requested identifier.</summary>
    public ProposalDecisionDto[] Results { get; set; } = [];

    /// <summary>Proposals still waiting in the requested scope.</summary>
    public ProposalDto[] Pending { get; set; } = [];
}
