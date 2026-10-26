using Api.Auth;
using Core.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Name and password of the signed-in account.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class ProfileController(IAccounts accounts) : ControllerBase
{
    /// <summary>
    /// Changes the display name.
    /// </summary>
    [HttpPost("update")]
    public Task<AccountDto> Update(UpdateNameRequest request, CancellationToken token)
        => accounts.UpdateNameAsync(AccountSignIn.RequireAccountId(User), request.Name, token);

    /// <summary>
    /// Changes the password.
    /// </summary>
    [HttpPost("password")]
    public async Task<IActionResult> Password(ChangePasswordRequest request, CancellationToken token)
    {
        await accounts.ChangePasswordAsync(
            AccountSignIn.RequireAccountId(User),
            request.CurrentPassword,
            request.Password,
            token);
        return NoContent();
    }

    /// <summary>New display name.</summary>
    public class UpdateNameRequest
    {
        /// <summary>Display name.</summary>
        public string? Name { get; set; }
    }

    /// <summary>Password change.</summary>
    public class ChangePasswordRequest
    {
        /// <summary>Current password.</summary>
        public string? CurrentPassword { get; set; }

        /// <summary>New password, 12 characters minimum.</summary>
        public string? Password { get; set; }
    }
}
