using System.Net;

namespace Core.Exceptions;

/// <summary>
/// Conflict (409).
/// </summary>
/// <param name="message"><c>ERR_*</c> code.</param>
public class ConflictException(string message) : Exception(message), IAppException
{
    /// <inheritdoc />
    public HttpStatusCode Code => HttpStatusCode.Conflict;
}
