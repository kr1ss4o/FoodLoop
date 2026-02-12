using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers
{
    [Authorize(Roles = "Restaurant")]
    public class OrderManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public OrderManagerController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // -------------------------------
        // Dashboard page
        // -------------------------------
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

            if (restaurant == null)
            {
                TempData["Error"] = "Restaurant not found.";
                return RedirectToAction("Index", "Home");
            }

            var reservations = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Offer)
                .Where(r => r.Offer.RestaurantId == restaurant.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View("/Views/Restaurant/OrderDashboard/Index.cshtml", reservations);
        }

        // -------------------------------
        // Confirm
        // -------------------------------
        [HttpPost]
        public async Task<IActionResult> Confirm(Guid id)
        {
            var r = await _context.Reservations.FindAsync(id);
            if (r == null) return RedirectToAction("Index");

            r.Status = ReservationStatus.Confirmed;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order confirmed!";
            return RedirectToAction("Index");
        }

        // -------------------------------
        // Cancel
        // -------------------------------
        [HttpPost]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var r = await _context.Reservations.FindAsync(id);
            if (r == null) return RedirectToAction("Index");

            r.Status = ReservationStatus.Canceled;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order canceled!";
            return RedirectToAction("Index");
        }

        // -------------------------------
        // Finish
        // -------------------------------
        [HttpPost]
        public async Task<IActionResult> Finish(Guid id)
        {
            var r = await _context.Reservations.FindAsync(id);
            if (r == null) return RedirectToAction("Index");

            r.Status = ReservationStatus.Finished;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order finished!";
            return RedirectToAction("Index");
        }
    }
}
