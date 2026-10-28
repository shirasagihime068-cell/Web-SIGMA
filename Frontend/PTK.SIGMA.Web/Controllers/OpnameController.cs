using Microsoft.AspNetCore.Mvc;

namespace PTK.SIGMA.Web.Controllers;

public sealed class OpnameController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult Arsip() => View();
}
