using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using PTK.SIGMA.Domain.SigmaService;

namespace PTK.SIGMA.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // ==========================================
    // 1. CEK USER YANG SUDAH TER-AUTENTIKASI
    // ==========================================
    // API membaca shared auth cookie yang sudah diterbitkan.
    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            success = true,
            data = new
            {
                username = User.Identity?.Name,
                email = User.FindFirst(ClaimTypes.Email)?.Value,
                role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Staff"
            }
        });
    }

    // ==========================================
    // 2. LOGIN LOKAL (EXISTING)
    // ==========================================
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [SupportedOSPlatform("windows")]
    public async Task<IActionResult> Login([FromBody] AuthModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = "Data input tidak valid.",
                errors = ModelState,
                newCaptchaImage = GenerateNewCaptchaBase64()
            });
        }

        // VALIDASI KETAT CAPTCHA (One-Time Use Policy)
        string? sessionCaptcha = HttpContext.Session.GetString("CaptchaCode");
        HttpContext.Session.Remove("CaptchaCode");

        if (string.IsNullOrEmpty(sessionCaptcha) ||
            string.IsNullOrEmpty(model.CaptchaCode) ||
            !sessionCaptcha.Equals(model.CaptchaCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                success = false,
                message = "Kode Captcha yang Anda masukkan salah atau sudah kedaluwarsa!",
                newCaptchaImage = GenerateNewCaptchaBase64()
            });
        }

        var result = await _authService.AuthenticateAsync(model.Email!, model.Password!);

        if (!result.Success)
        {
            return Unauthorized(new
            {
                success = false,
                message = result.Message,
                newCaptchaImage = GenerateNewCaptchaBase64()
            });
        }

        var userDto = result.Data;
        string username = !string.IsNullOrEmpty(userDto?.Username) ? userDto.Username : model.Email!;
        string email = userDto?.Email ?? model.Email!;
        string role = !string.IsNullOrEmpty(userDto?.Role) ? userDto.Role : "Staff";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("PTK.SIGMA.RoleSource", "LocalDatabase")
        };

        var claimsIdentity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity)
        );

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = userDto
        });
    }

    // ==========================================
    // 3. LOGOUT
    // ==========================================
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return Ok(new
        {
            success = true,
            message = "Logout berhasil."
        });
    }

    // ==========================================
    // 4. HELPER CAPTCHA
    // ==========================================
    [HttpGet("GetCaptchaImage")]
    [AllowAnonymous]
    [EnableRateLimiting("captcha")]
    [SupportedOSPlatform("windows")]
    public IActionResult GetCaptchaImage()
    {
        string code = GenerateRandomCaptchaText(5);
        HttpContext.Session.SetString("CaptchaCode", code);

        byte[] imgBytes = GenerateImageBytes(code);
        return File(imgBytes, "image/png");
    }

    private string GenerateRandomCaptchaText(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)])
            .ToArray());
    }

    [SupportedOSPlatform("windows")]
    private byte[] GenerateImageBytes(string text)
    {
        using (var bitmap = new Bitmap(160, 60))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.FromArgb(245, 247, 250));
            var rand = new Random();

            for (int i = 0; i < 150; i++)
            {
                int x = rand.Next(bitmap.Width);
                int y = rand.Next(bitmap.Height);
                bitmap.SetPixel(
                    x,
                    y,
                    Color.FromArgb(
                        rand.Next(120, 200),
                        rand.Next(120, 200),
                        rand.Next(120, 200)));
            }

            using (var pen = new Pen(Color.FromArgb(170, 180, 195), 1.3f))
            {
                for (int i = 0; i < 6; i++)
                {
                    graphics.DrawLine(
                        pen,
                        rand.Next(bitmap.Width),
                        rand.Next(bitmap.Height),
                        rand.Next(bitmap.Width),
                        rand.Next(bitmap.Height));
                }
            }

            float xPos = 12;
            foreach (char c in text)
            {
                using (var font = new Font(
                    "Verdana",
                    rand.Next(19, 24),
                    FontStyle.Bold))
                {
                    GraphicsState gstate = graphics.Save();
                    float yPos = rand.Next(6, 16);

                    graphics.TranslateTransform(xPos, yPos);

                    float angle = rand.Next(-25, 25);
                    graphics.RotateTransform(angle);

                    var color = Color.FromArgb(
                        rand.Next(10, 80),
                        rand.Next(10, 80),
                        rand.Next(10, 80));

                    using (var brush = new SolidBrush(color))
                    {
                        graphics.DrawString(
                            c.ToString(),
                            font,
                            brush,
                            0,
                            0);
                    }

                    graphics.Restore(gstate);
                }

                xPos += 28;
            }

            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private string GenerateNewCaptchaBase64()
    {
        string code = GenerateRandomCaptchaText(5);
        HttpContext.Session.SetString("CaptchaCode", code);

        byte[] imgBytes = GenerateImageBytes(code);

        return "data:image/png;base64," +
               Convert.ToBase64String(imgBytes);
    }
}
