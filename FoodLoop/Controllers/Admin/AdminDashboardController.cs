using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodLoop.Data;

namespace FoodLoop.Controllers.Admin;

[Authorize(Roles = "Admin")]
public class AdminDashboardController : AdminBaseController
{
    private readonly ApplicationDbContext _context;

    public AdminDashboardController(ApplicationDbContext context)
    : base(context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        Info("Добре дошли в администраторския панел.");

        var model = new
        {
            Restaurants = await _context.Restaurants.CountAsync(),
            Offers = await _context.Offers.CountAsync(),
            Reservations = await _context.Reservations.CountAsync(),
            Reviews = await _context.Reviews.CountAsync(),
            Categories = await _context.Categories.CountAsync(),
            Tags = await _context.Tags.CountAsync()
        };

        return View("~/Views/Admin/Dashboard.cshtml", model);
    }
}