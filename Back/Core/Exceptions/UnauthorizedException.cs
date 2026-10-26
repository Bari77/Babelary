using System.Net;

namespace Core.Exceptions;

/// <summary>
/// Unauthorized (401).
/// </summary>
/// <param name="message"><c>ERR_*</c> code.</param>
public class UnauthorizedException(string message) : Exception(message), IAppException
{
    /// <inheritdoc />
    public HttpStatusCode Code => HttpStatusCode.Unauthorized;
}
