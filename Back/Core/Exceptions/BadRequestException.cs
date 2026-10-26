using System.Net;

namespace Core.Exceptions;

/// <summary>
/// Bad request (400).
/// </summary>
/// <param name="message"><c>ERR_*</c> code.</param>
/// <param name="detail">Extra value the client can show, such as a token name.</param>
public class BadRequestException(string message, string? detail = null) : Exception(message), IAppException
{
    /// <inheritdoc />
    public HttpStatusCode Code => HttpStatusCode.BadRequest;

    /// <summary>Optional value paired with <paramref name="message"/>.</summary>
    public string? Detail { get; } = detail;
}
