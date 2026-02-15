using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.Enums;
using FoodLoop.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public OrdersController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // MAIN ORDERS DASHBOARD
        // =========================================================
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            // Cart items
            var cartItems = await _context.CartItems
                .Where(c => c.UserId == user.Id)
                .Include(c => c.Offer)
                    .ThenInclude(o => o.Restaurant)
                .ToListAsync();

            // Reservations (multi-item structure)
            var reservations = await _context.Reservations
                .Where(r => r.UserId == user.Id)
                .Include(r => r.User)
                .Include(r => r.Items)
                    .ThenInclude(i => i.Offer)
                        .ThenInclude(o => o.Restaurant)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var model = new CartPageViewModel
            {
                CartItems = cartItems,
                Reservations = reservations
            };

            return View(model);
        }
    }
}