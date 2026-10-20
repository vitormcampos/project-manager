using Microsoft.AspNetCore.Mvc;

namespace ProjectHub.Api.Controllers;

/// <summary>
/// Provides the API home and liveness endpoints.
/// </summary>
[ApiController]
[Route("health")]
public sealed class HomeController : ControllerBase
{
    /// <summary>Returns the current health status of the API.</summary>
    /// <returns>HTTP 200 with a healthy status.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth() => Ok(new { status = "Healthy" });
}
