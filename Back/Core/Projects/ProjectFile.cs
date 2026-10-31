namespace Core.Projects;

/// <summary>
/// A named JSON file of a project, such as <c>account.json</c>. The same name exists in every locale.
/// </summary>
public class ProjectFile
{
    /// <summary>Identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Project that owns this file.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Nested JSON, the default for existing files.</summary>
    public const string NestedCatalog = "nested";

    /// <summary>Angular catalog: <c>locale</c> plus a flat <c>translations</c> map.</summary>
    public const string AngularCatalog = "angular";

    /// <summary>File name, without a directory.</summary>
    public string Name { get; set; } = "";

    /// <summary><see cref="NestedCatalog"/> or <see cref="AngularCatalog"/>.</summary>
    public string Catalog { get; set; } = NestedCatalog;

    /// <summary>Creation instant, UTC.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last change instant, UTC.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Project.</summary>
    public Project Project { get; set; } = null!;

    /// <summary>Keys of this file, in source order.</summary>
    public List<ProjectKey> Keys { get; set; } = [];
}
