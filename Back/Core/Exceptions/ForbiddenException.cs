using System.Net;

namespace Core.Exceptions;

/// <summary>
/// Forbidden (403).
/// </summary>
/// <param name="message"><c>ERR_*</c> code.</param>
public class ForbiddenException(string message) : Exception(message), IAppException
{
    /// <inheritdoc />
    public HttpStatusCode Code => HttpStatusCode.Forbidden;
}
