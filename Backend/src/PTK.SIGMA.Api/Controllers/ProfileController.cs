using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Security.Claims;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
            return NotFound(new { success = false, message = "Sesi pengguna tidak valid." });

        var result = await _profileService.GetProfileAsync(email);
        if (result == null)
            return NotFound(new { success = false, message = "Profil pengguna tidak ditemukan." });

        return Ok(new { success = true, data = result });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UserProfileUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Data input tidak valid", errors = ModelState });

        var email = User.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
            return BadRequest(new { success = false, message = "Sesi pengguna tidak valid." });

        var result = await _profileService.UpdateProfileAsync(email, dto);
        if (!result.Success)
            return NotFound(new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message });
    }
}
