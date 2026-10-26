namespace Core.Exceptions;

/// <summary>
/// JSON body written by the exception middleware. Property names stay PascalCase.
/// </summary>
public class ExceptionResult
{
    /// <summary>
    /// <c>ERR_*</c> code, or a generic message for an unknown failure.
    /// </summary>
    public string Message { get; set; } = "ERR_INTERNAL";

    /// <summary>
    /// Stack trace outside production.
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Request trace identifier.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Extra value for the message, such as the expected token.
    /// </summary>
    public string? Detail { get; set; }
}
