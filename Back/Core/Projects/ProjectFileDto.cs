namespace Core.Projects;

/// <summary>
/// Zip of one target locale. The download name is the locale code.
/// Each entry keeps the source file name and the source key shape. Only the leaf texts change.
/// </summary>
public class LocaleExport
{
    /// <summary>Download name, such as <c>fr-FR.zip</c>.</summary>
    public string FileName { get; set; } = "";

    /// <summary>Zip bytes.</summary>
    public byte[] Content { get; set; } = [];
}

/// <summary>A source file listed on the project.</summary>
public class ProjectFileDto
{
    /// <summary>File name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Number of string leaves.</summary>
    public int KeyCount { get; set; }
}

/// <summary>One uploaded source file.</summary>
public class SourceFileUpload
{
    /// <summary>File name, such as <c>account.json</c>.</summary>
    public string? Name { get; set; }

    /// <summary>Raw JSON text.</summary>
    public string? Content { get; set; }
}

/// <summary>What an import did to one file. <see cref="Error"/> is an <c>ERR_*</c> code when the file was skipped.</summary>
public class ImportFileDto
{
    /// <summary>File name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Keys created.</summary>
    public int Created { get; set; }

    /// <summary>Source values changed.</summary>
    public int Updated { get; set; }

    /// <summary>Keys removed because they left the file.</summary>
    public int Deleted { get; set; }

    /// <summary><c>ERR_*</c> when this file was not imported.</summary>
    public string? Error { get; set; }
}

/// <summary>Result of importing one or more source files.</summary>
public class ImportSourcesDto
{
    /// <summary>One entry per uploaded file, in the same order.</summary>
    public ImportFileDto[] Files { get; set; } = [];

    /// <summary>Project after the files that could be read were stored.</summary>
    public ProjectDto Project { get; set; } = new();
}

/// <summary>One key the translation import refused. The other keys of the file are still applied.</summary>
public class ImportKeyIssueDto
{
    /// <summary>Dotted path.</summary>
    public string Path { get; set; } = "";

    /// <summary><c>ERR_*</c> code.</summary>
    public string Error { get; set; } = "";

    /// <summary>Token named in the message, such as <c>{{name}}</c>.</summary>
    public string? Detail { get; set; }
}

/// <summary>What a translation import did to one file.</summary>
public class ImportTranslationFileDto
{
    /// <summary>File name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Saved values replaced.</summary>
    public int Updated { get; set; }

    /// <summary>Leaves already equal to the saved value.</summary>
    public int Unchanged { get; set; }

    /// <summary>Empty leaves that left the saved value in place.</summary>
    public int Skipped { get; set; }

    /// <summary>Leaves that are not keys of this source file.</summary>
    public int Ignored { get; set; }

    /// <summary>Source keys absent from this file. They stay untranslated by this import.</summary>
    public int Missing { get; set; }

    /// <summary>Keys refused, usually because a token does not match the source.</summary>
    public ImportKeyIssueDto[] Issues { get; set; } = [];

    /// <summary><c>ERR_*</c> when this file was not imported.</summary>
    public string? Error { get; set; }
}

/// <summary>Result of importing translation files for one target locale.</summary>
public class ImportTranslationsDto
{
    /// <summary>One entry per uploaded file, in the same order.</summary>
    public ImportTranslationFileDto[] Files { get; set; } = [];

    /// <summary>Project after the files that could be read were stored.</summary>
    public ProjectDto Project { get; set; } = new();
}

/// <summary>Progress of one target locale.</summary>
public class LocaleProgressDto
{
    /// <summary>Locale code.</summary>
    public string Locale { get; set; } = "";

    /// <summary>Values that are translated, identical, or to review.</summary>
    public int Translated { get; set; }

    /// <summary>Keys of the project.</summary>
    public int Total { get; set; }

    /// <summary>Proposals still waiting for the owner.</summary>
    public int Pending { get; set; }

    /// <summary>Waiting proposals whose key has no saved text in this locale.</summary>
    public int PendingOpen { get; set; }
}
