using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PTK.SIGMA.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class VendorController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
