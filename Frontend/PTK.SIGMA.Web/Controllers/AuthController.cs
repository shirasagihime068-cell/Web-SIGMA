using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace PTK.SIGMA.Web.Controllers;

public sealed class AuthController : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] =
            IsSafeReturnUrl(returnUrl)
                ? returnUrl
                : "/Home/Index";

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        // Clear the shared SIGMA authentication cookie and return to login.
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return Redirect("/Auth/Login");
    }

    private static bool IsSafeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) &&
               returnUrl.StartsWith('/') &&
               !returnUrl.StartsWith("//");
    }
}
