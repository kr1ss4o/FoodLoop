using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.Enums;
using FoodLoop.Models.ViewModels;
using FoodLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers
{
    [Authorize(Roles = "Restaurant")]
    public class RestaurantDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IDashboardAnalyticsService _analyticsService;

        public RestaurantDashboardController(ApplicationDbContext context, UserManager<User> userManager, IDashboardAnalyticsService analyticsService)
        {
            _context = context;
            _userManager = userManager;
            _analyticsService = analyticsService;
        }

        public async Task<IActionResult> Index(DateTime? date)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var vm = await _analyticsService.BuildDashboardAsync(user.Id, date);

            return View(vm);
        }
    }
}