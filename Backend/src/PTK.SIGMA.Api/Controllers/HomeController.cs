using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/home")]
public sealed class HomeApiController : ControllerBase
{
    private readonly IHomeService _homeService;

    public HomeApiController(IHomeService homeService)
    {
        _homeService = homeService;
    }

    [HttpGet("user-info")]
    public IActionResult GetUserInfo()
    {
        var currentName = User.Identity?.Name ?? "Administrator";
        var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Staff";
        var isAdmin = currentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        return Ok(new { success = true, name = currentName, role = currentRole, isAdmin });
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] string? search = "",
        [FromQuery] string? category = "",
        [FromQuery] string? filter = "",
        [FromQuery] int? page = 1,
        [FromQuery] int? pageSize = 8,
        [FromQuery] string? sort = "terbaru",
        CancellationToken cancellationToken = default)
    {
        var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Staff";
        var isAdmin = currentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        var result = await _homeService.GetDashboardAsync(
            search,
            category,
            filter,
            page ?? 1,
            pageSize ?? 8,
            sort,
            isAdmin,
            cancellationToken);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Staff";
        var isAdmin = currentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);
        var result = await _homeService.GetSummaryAsync(isAdmin, cancellationToken);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventory(
        [FromQuery] string? search = "",
        [FromQuery] string? category = "",
        [FromQuery] string? filter = "",
        [FromQuery] int? page = 1,
        [FromQuery] int? pageSize = 8,
        [FromQuery] string? sort = "terbaru",
        CancellationToken cancellationToken = default)
    {
        var result = await _homeService.GetInventoryAsync(
            search,
            category,
            filter,
            page ?? 1,
            pageSize ?? 8,
            sort,
            cancellationToken);
        return Ok(new { success = true, data = result });
    }
}
