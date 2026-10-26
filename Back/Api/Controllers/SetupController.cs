using Api.Auth;
using Core.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// First-account installation. The actions refuse to run once an account exists.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/v1/[controller]")]
public class SetupController(IAccounts accounts) : ControllerBase
{
    /// <summary>
    /// Reports whether the instance owner already exists.
    /// </summary>
    [HttpGet("status")]
    public Task<SetupStatusDto> Status(CancellationToken token)
        => accounts.GetSetupStatusAsync(token);

    /// <summary>
    /// Creates the instance owner and opens a session.
    /// </summary>
    [HttpPost("create")]
    public async Task<AccountDto> Create(CreateOwnerRequest request, CancellationToken token)
    {
        var account = await accounts.CreateOwnerAsync(request.Name, request.Email, request.Password, token);
        var sessionId = await accounts.OpenSessionAsync(account.Id, token);
        await AccountSignIn.SignInAsync(HttpContext, account, sessionId);
        return account;
    }

    /// <summary>Installation form.</summary>
    public class CreateOwnerRequest
    {
        /// <summary>Display name.</summary>
        public string? Name { get; set; }

        /// <summary>Login.</summary>
        public string? Email { get; set; }

        /// <summary>Password, 12 characters minimum.</summary>
        public string? Password { get; set; }
    }
}
