using System.Net;

namespace Core.Exceptions;

/// <summary>
/// Not found (404).
/// </summary>
/// <param name="message"><c>ERR_*</c> code.</param>
public class NotFoundException(string message) : Exception(message), IAppException
{
    /// <inheritdoc />
    public HttpStatusCode Code => HttpStatusCode.NotFound;
}
