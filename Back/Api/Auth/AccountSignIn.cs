using System.Security.Claims;
using Core.Accounts;
using Core.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Api.Auth;

/// <summary>
/// Cookie ticket for a web session.
/// </summary>
public static class AccountSignIn
{
    /// <summary>Claim holding the server session id.</summary>
    public const string SessionId = "session_id";

    /// <summary>Reads the account id or refuses the request.</summary>
    public static Guid RequireAccountId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(raw, out var id))
        {
            throw new UnauthorizedException("ERR_UNAUTHORIZED");
        }

        return id;
    }

    /// <summary>Reads the session id when the ticket is a web session.</summary>
    public static Guid? ReadSessionId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(SessionId);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    /// <summary>Issues the session cookie.</summary>
    public static Task SignInAsync(HttpContext http, AccountDto account, Guid sessionId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.Name),
            new(ClaimTypes.Email, account.Email),
            new(SessionId, sessionId.ToString()),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
            });
    }
}
