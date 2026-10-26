using Core.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Process health.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/v1/[controller]")]
public class HealthController(IHealth health) : ControllerBase
{
    /// <summary>
    /// Reports whether the API can reach the database.
    /// </summary>
    [HttpGet("get")]
    public async Task<IActionResult> Get(CancellationToken token)
    {
        var result = await health.GetAsync(token);
        if (!result.Database)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        }

        return Ok(result);
    }
}
