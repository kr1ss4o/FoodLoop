using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodLoop.Data;

namespace FoodLoop.Controllers.Admin;

[Authorize(Roles = "Admin")]
public abstract class AdminBaseController : Controller
{
    protected readonly ApplicationDbContext _context;

    protected AdminBaseController(ApplicationDbContext context)
    {
        _context = context;
    }

    protected IActionResult AdminView(string view, object model)
    {
        return View($"~/Views/Admin/{view}.cshtml", model);
    }

    protected IActionResult AdminCreate(object model)
    {
        return View("~/Views/Admin/Create.cshtml", model);
    }

    protected IActionResult AdminEdit(object model)
    {
        return View("~/Views/Admin/Edit.cshtml", model);
    }

    protected IActionResult AdminDetails(object model)
    {
        return View("~/Views/Admin/Details.cshtml", model);
    }

    protected IActionResult AdminRedirect(string action)
    {
        return RedirectToAction(action);
    }
}