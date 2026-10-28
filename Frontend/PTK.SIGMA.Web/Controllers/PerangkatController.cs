using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PTK.SIGMA.Web.Controllers;

public sealed class PerangkatController : Controller
{
    [HttpGet]
    public IActionResult Index() => RedirectToAction("Index", "Home");

    [HttpGet]
    public IActionResult Create() => View();

    [HttpGet]
    public IActionResult Edit(int? id) => View();

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Delete(int? id) => View();

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Arsip() => View();

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult Validasi() => View();

    [HttpGet]
    public IActionResult CetakLaporan() => View();
}
