using Microsoft.AspNetCore.Mvc;

namespace PTK.SIGMA.Web.Controllers;

public sealed class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult Privacy() => View();

    [HttpGet]
    public IActionResult Error()
    {
        return View(new PTK.SIGMA.Web.Models.ErrorViewModel
        {
            RequestId = HttpContext.TraceIdentifier
        });
    }
}
