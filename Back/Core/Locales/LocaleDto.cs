namespace Core.Locales;

/// <summary>
/// One locale the interface can offer.
/// </summary>
public class LocaleDto
{
    /// <summary>
    /// Stable code, such as <c>fr-FR</c>. The interface shows the language name, not this code.
    /// </summary>
    public string Code { get; set; } = "";
}
