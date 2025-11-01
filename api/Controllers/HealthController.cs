using Microsoft.AspNetCore.Mvc;

namespace LexiFlow.Api.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet("/healthz")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new { status = "ok" });
}
