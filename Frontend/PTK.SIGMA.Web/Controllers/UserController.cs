using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PTK.SIGMA.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class UserController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult Create() => View();

    [HttpGet]
    public IActionResult Edit(int? id) => View();

    [HttpGet]
    public IActionResult Delete(int? id) => View();
}
