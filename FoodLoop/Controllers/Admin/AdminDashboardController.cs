using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FoodLoop.Data;
using FoodLoop.Models.Entities;

namespace FoodLoop.Controllers.Admin;

[Authorize(Roles = "Admin")]
public class AdminDashboardController : AdminBaseController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public AdminDashboardController(
        ApplicationDbContext context,
        UserManager<User> userManager)
        : base(context)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        Info("Добре дошли в администраторския панел.");

        var clientUsers = await _userManager.GetUsersInRoleAsync("Client");

        var model = new
        {
            Restaurants = await _context.Restaurants.CountAsync(),
            Offers = await _context.Offers.CountAsync(),
            Reservations = await _context.Reservations.CountAsync(),
            Reviews = await _context.Reviews.CountAsync(),
            Categories = await _context.Categories.CountAsync(),
            Tags = await _context.Tags.CountAsync(),
            Clients = clientUsers.Count
        };

        return View("~/Views/Admin/Dashboard.cshtml", model);
    }
}