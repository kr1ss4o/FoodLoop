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

        // =========================================================
        // DASHBOARD
        // =========================================================
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

            // NEW MULTI-ITEM STRUCTURE
            var reservations = await _context.Reservations
            .Include(r => r.User)
            .Include(r => r.Items)
            .ThenInclude(i => i.Offer)
            .Where(r => r.Items.Any(i => i.Offer.RestaurantId == restaurant.Id))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

            return View("/Views/Restaurant/OrderDashboard/Index.cshtml", reservations);
        }

        // =========================================================
        // CONFIRM
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> Confirm(Guid id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
                return RedirectToAction("Index");

            reservation.Status = ReservationStatus.Confirmed;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order confirmed!";
            return RedirectToAction("Index");
        }

        // =========================================================
        // CANCEL
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
                return RedirectToAction("Index");

            reservation.Status = ReservationStatus.Canceled;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order canceled!";
            return RedirectToAction("Index");
        }

        // =========================================================
        // FINISH
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> Finish(Guid id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
                return RedirectToAction("Index");

            reservation.Status = ReservationStatus.Finished;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order finished!";
            return RedirectToAction("Index");
        }

        // -------------------------------
        // Mark as Out For Delivery
        // -------------------------------
        [HttpPost]
        public async Task<IActionResult> OutForDelivery(Guid id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null) return RedirectToAction("Index");

            reservation.Status = ReservationStatus.OutForDelivery;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order is now out for delivery!";
            return RedirectToAction("Index");
        }

    }
}