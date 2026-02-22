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
            if (user == null)
                return RedirectToAction("Login", "Account");

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

            if (restaurant == null)
            {
                TempData["Error"] = "Restaurant not found.";
                return RedirectToAction("Login", "Account");
            }

            // LOW STOCK ALERT
            var lowStockOffers = await _context.Offers
                .Where(o => o.RestaurantId == restaurant.Id && o.QuantityAvailable <= 3)
                .ToListAsync();

            ViewBag.LowStockCount = lowStockOffers.Count;

            var expirationTime = DateTime.UtcNow.AddMinutes(-30);

            var expiredReservations = await _context.Reservations
                .Where(r =>
                    r.Status == ReservationStatus.Pending &&
                    r.CreatedAt <= expirationTime &&
                    r.Items.Any(i => i.Offer.RestaurantId == restaurant.Id))
                .ToListAsync();

            foreach (var reservation in expiredReservations)
            {
                reservation.Status = ReservationStatus.Canceled;
            }

            if (expiredReservations.Any())
                await _context.SaveChangesAsync();

            var reservations = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Items)
                    .ThenInclude(i => i.Offer)
                .Include(r => r.StatusLogs)
                .Where(r => r.Items.Any(i => i.Offer.RestaurantId == restaurant.Id))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View("/Views/Restaurant/OrderDashboard/Index.cshtml", reservations);
        }

        // =========================================================
        // ACTIONS
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(Guid id)
            => await ChangeStatus(id, ReservationStatus.Confirmed);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OutForDelivery(Guid id)
            => await ChangeStatus(id, ReservationStatus.OutForDelivery);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finish(Guid id)
            => await ChangeStatus(id, ReservationStatus.Finished);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(Guid id)
            => await ChangeStatus(id, ReservationStatus.Canceled);

        // -------------------------------
        // Shared transition logic
        // -------------------------------
        private async Task<IActionResult> ChangeStatus(Guid reservationId, ReservationStatus newStatus)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

            if (restaurant == null)
            {
                TempData["Error"] = "Restaurant not found.";
                return RedirectToAction(nameof(Index));
            }

            var reservation = await _context.Reservations
                .Include(r => r.Items)
                    .ThenInclude(i => i.Offer)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }

            var belongsToRestaurant = reservation.Items.Any(i => i.Offer.RestaurantId == restaurant.Id);
            if (!belongsToRestaurant)
                return Forbid();

            if (!CanTransition(reservation, newStatus))
            {
                TempData["Error"] = "Invalid status transition.";
                return RedirectToAction(nameof(Index));
            }

            var oldStatus = reservation.Status;

            reservation.Status = newStatus;

            // Restore stock ONLY when canceling
            if (newStatus == ReservationStatus.Canceled &&
                oldStatus != ReservationStatus.Canceled)
            {
                foreach (var item in reservation.Items)
                {
                    if (item.Offer != null)
                    {
                        item.Offer.QuantityAvailable += item.Quantity;
                    }
                }
            }

            _context.ReservationStatusLogs.Add(new ReservationStatusLog
            {
                ReservationId = reservation.Id,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedByUserId = user.Id
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = newStatus switch
            {
                ReservationStatus.Confirmed => "Order confirmed!",
                ReservationStatus.OutForDelivery => "Order is now out for delivery!",
                ReservationStatus.Finished => "Order finished!",
                ReservationStatus.Canceled => "Order canceled!",
                _ => "Updated."
            };

            return RedirectToAction(nameof(Index));
        }

        private static bool CanTransition(Reservation reservation, ReservationStatus newStatus)
        {
            // Delivery-specific rule: OutForDelivery makes sense only for Delivery orders
            if (newStatus == ReservationStatus.OutForDelivery &&
                !string.Equals(reservation.DeliveryType, "Delivery", StringComparison.OrdinalIgnoreCase))
                return false;

            return reservation.Status switch
            {
                ReservationStatus.Pending => newStatus is ReservationStatus.Confirmed or ReservationStatus.Canceled,
                ReservationStatus.Confirmed => newStatus is ReservationStatus.OutForDelivery or ReservationStatus.Finished or ReservationStatus.Canceled,
                ReservationStatus.OutForDelivery => newStatus is ReservationStatus.Finished,
                ReservationStatus.Finished => false,
                ReservationStatus.Canceled => false,
                _ => false
            };
        }
    }
}
