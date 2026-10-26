using Api.Auth;
using Core.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Personal CLI tokens of the signed-in account.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class TokensController(IAccounts accounts) : ControllerBase
{
    /// <summary>
    /// Lists active tokens. Secrets are not included.
    /// </summary>
    [HttpGet("list")]
    public Task<IReadOnlyList<TokenDto>> List(CancellationToken token)
        => accounts.ListTokensAsync(AccountSignIn.RequireAccountId(User), token);

    /// <summary>
    /// Creates a token and returns its secret once.
    /// </summary>
    [HttpPost("create")]
    public Task<CreatedTokenDto> Create(CreateTokenRequest request, CancellationToken token)
        => accounts.CreateTokenAsync(AccountSignIn.RequireAccountId(User), request.Name, token);

    /// <summary>
    /// Revokes a token. Later bearer calls with that secret are refused.
    /// </summary>
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken token)
    {
        await accounts.RevokeTokenAsync(AccountSignIn.RequireAccountId(User), id, token);
        return NoContent();
    }

    /// <summary>New token name.</summary>
    public class CreateTokenRequest
    {
        /// <summary>Name shown in the profile.</summary>
        public string? Name { get; set; }
    }
}
