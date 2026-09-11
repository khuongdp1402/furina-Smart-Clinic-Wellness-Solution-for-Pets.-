using Furina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Furina.Api.Controllers;

/// <summary>
/// TASK-13 AC-1: "docker compose up ... /health trả 200". Exempted from
/// TenantResolutionMiddleware (see ExemptPathPrefixes) since a health probe
/// has no tenant. Checks real DB connectivity rather than just "the process
/// is alive" — a container whose API can't reach Postgres should not read
/// as healthy.
/// </summary>
[ApiController]
[Route("health")]
public class HealthController(FurinaDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var canConnect = await db.Database.CanConnectAsync(ct);
        if (!canConnect)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "unhealthy", database = "unreachable" });
        }

        return Ok(new { status = "healthy", hotReloadCheck = "TASK-13-AC-3" });
    }
}
