using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PTK.SIGMA.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class SystemController : ControllerBase
{
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "ok",
            service = "PTK.SIGMA backend"
        });
    }

    [HttpGet("session")]
    [Authorize]
    public IActionResult Session()
    {
        return Ok(new
        {
            authenticated = true,
            username = User.Identity?.Name,
            fullName = User.FindFirstValue("FullName"),
            role = User.FindFirstValue(ClaimTypes.Role)
        });
    }
}
