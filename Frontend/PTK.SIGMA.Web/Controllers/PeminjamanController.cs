using Microsoft.AspNetCore.Mvc;

namespace PTK.SIGMA.Web.Controllers;

public sealed class PeminjamanController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult Create() => View();

    [HttpGet]
    public IActionResult Edit(int? id) => View();

    [HttpGet]
    public IActionResult Delete(int? id) => View();

    [HttpGet]
    public IActionResult PrintBa(int? id) => View();
}
