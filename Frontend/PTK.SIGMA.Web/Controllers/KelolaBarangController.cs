using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PTK.SIGMA.Web.Controllers;

public sealed class KelolaBarangController : Controller
{
    [HttpGet] public IActionResult Index() => View();
    [HttpGet] public IActionResult Masuk() => View();
    [HttpGet] public IActionResult Keluar() => View();
    [HttpGet, Authorize(Roles = "Admin")] public IActionResult Kategori() => View();
    [HttpGet, Authorize(Roles = "Admin")] public IActionResult CreateKategori() => RedirectToAction(nameof(Kategori));
    [HttpGet, Authorize(Roles = "Admin")] public IActionResult Lokasi() => View();
    [HttpGet, Authorize(Roles = "Admin")] public IActionResult Rak() => View();
}
