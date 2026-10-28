using Microsoft.AspNetCore.Mvc;

namespace PTK.SIGMA.Web.Controllers;

public sealed class ProfileController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
