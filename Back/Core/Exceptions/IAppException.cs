using System.Net;

namespace Core.Exceptions;

/// <summary>
/// Business error mapped to an HTTP status. The exception message is a stable <c>ERR_*</c> code.
/// </summary>
public interface IAppException
{
    /// <summary>
    /// HTTP status sent to the client.
    /// </summary>
    HttpStatusCode Code { get; }
}
