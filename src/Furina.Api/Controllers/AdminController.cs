using Furina.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Furina.Api.Controllers;

/// <summary>
/// Sample Owner-only endpoint used to exercise TASK-12 AC-4 (role
/// Receptionist calling an Owner-only endpoint gets 403). Real
/// owner-restricted endpoints (billing, staff management, ...) land here
/// or in their own controllers in later sprints.
/// </summary>
[ApiController]
[Route("admin")]
[Authorize(Policy = Policies.OwnerOnly)]
public class AdminController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { pong = true });
}
