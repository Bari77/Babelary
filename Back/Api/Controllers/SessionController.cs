using Api.Auth;
using Core.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Web session: login, logout, and the current account.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class SessionController(IAccounts accounts) : ControllerBase
{
    /// <summary>
    /// Checks the password and opens a session cookie.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<AccountDto> Login(LoginRequest request, CancellationToken token)
    {
        var account = await accounts.LoginAsync(request.Email, request.Password, token);
        var sessionId = await accounts.OpenSessionAsync(account.Id, token);
        await AccountSignIn.SignInAsync(HttpContext, account, sessionId);
        return account;
    }

    /// <summary>
    /// Deletes the server session and clears the cookie.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken token)
    {
        var sessionId = AccountSignIn.ReadSessionId(User);
        if (sessionId is Guid id)
        {
            await accounts.CloseSessionAsync(id, token);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    /// <summary>
    /// Returns the account of the cookie or bearer token.
    /// </summary>
    [HttpGet("me")]
    public Task<AccountDto> Me(CancellationToken token)
        => accounts.GetAsync(AccountSignIn.RequireAccountId(User), token);

    /// <summary>Login form.</summary>
    public class LoginRequest
    {
        /// <summary>Login.</summary>
        public string? Email { get; set; }

        /// <summary>Password.</summary>
        public string? Password { get; set; }
    }
}
