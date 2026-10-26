namespace Core.Projects;

/// <summary>
/// Translation of one key in one target locale. An empty text means the key is still to translate.
/// </summary>
public class ProjectValue
{
    /// <summary>Identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Key this value translates.</summary>
    public Guid KeyId { get; set; }

    /// <summary>Target locale, <c>xx-XX</c>.</summary>
    public string Locale { get; set; } = "";

    /// <summary>Translated text. Empty means not translated.</summary>
    public string Text { get; set; } = "";

    /// <summary>Source text this translation was saved against. A later source change marks it for review.</summary>
    public string SourceSnapshot { get; set; } = "";

    /// <summary>Last change instant, UTC.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Key.</summary>
    public ProjectKey Key { get; set; } = null!;
}
