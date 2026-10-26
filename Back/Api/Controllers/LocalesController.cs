using Core.Locales;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Locales a project may use.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class LocalesController : ControllerBase
{
    /// <summary>Lists every accepted locale code.</summary>
    [HttpGet("list")]
    public IReadOnlyList<LocaleDto> List()
        => LocaleCatalog.All.Select(code => new LocaleDto { Code = code }).ToArray();
}
