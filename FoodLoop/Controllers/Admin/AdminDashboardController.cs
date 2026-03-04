using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodLoop.Data;

namespace FoodLoop.Controllers.Admin;

[Authorize(Roles = "Admin")]
public class AdminDashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminDashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new
        {
            Restaurants = await _context.Restaurants.CountAsync(),
            Offers = await _context.Offers.CountAsync(),
            Reservations = await _context.Reservations.CountAsync(),
            Reviews = await _context.Reviews.CountAsync()
        };

        return View("~/Views/Admin/Dashboard.cshtml", model);
    }
}