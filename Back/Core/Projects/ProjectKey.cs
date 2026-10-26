namespace Core.Projects;

/// <summary>
/// A translatable leaf. <c>ACCOUNT.LOGIN</c> is a key. <c>ACCOUNT</c> is only a group.
/// </summary>
public class ProjectKey
{
    /// <summary>Identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>File that contains this key.</summary>
    public Guid FileId { get; set; }

    /// <summary>Dotted path of the string leaf.</summary>
    public string Path { get; set; } = "";

    /// <summary>Source text. Leading and trailing spaces are part of the value.</summary>
    public string SourceValue { get; set; } = "";

    /// <summary>Position in the source file. Pull keeps this order.</summary>
    public int Ordinal { get; set; }

    /// <summary>Creation instant, UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last change of the source text, UTC.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>File.</summary>
    public ProjectFile File { get; set; } = null!;

    /// <summary>One value per target locale.</summary>
    public List<ProjectValue> Values { get; set; } = [];
}
