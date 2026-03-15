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
                TempData["Error"] = "Не беше намерен ресторант..";
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
                TempData["Error"] = "Не беше намерен ресторант.";
                return RedirectToAction(nameof(Index));
            }

            var reservation = await _context.Reservations
                .Include(r => r.Items)
                    .ThenInclude(i => i.Offer)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
            {
                TempData["Error"] = "Не беше намерена поръчка.";
                return RedirectToAction(nameof(Index));
            }

            var belongsToRestaurant = reservation.Items.Any(i => i.Offer.RestaurantId == restaurant.Id);
            if (!belongsToRestaurant)
                return Forbid();

            if (!CanTransition(reservation, newStatus))
            {
                TempData["Error"] = "Невалидна промяна на статус.";
                return RedirectToAction(nameof(Index));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var oldStatus = reservation.Status;

                reservation.Status = newStatus;

                if (newStatus == ReservationStatus.Canceled &&
                    oldStatus != ReservationStatus.Canceled)
                {
                    foreach (var item in reservation.Items)
                    {
                        var offer = await _context.Offers
                            .FirstOrDefaultAsync(o => o.Id == item.OfferId);

                        if (offer != null)
                        {
                            offer.QuantityAvailable += item.Quantity;
                        }
                    }
                }

                _context.ReservationStatusLogs.Add(new ReservationStatusLog
                {
                    ReservationId = reservation.Id,
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    ChangedByUserId = user.Id,
                    ChangedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Неочаквана грешка.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = newStatus switch
            {
                ReservationStatus.Confirmed => "Поръчката бе потвърдена.",
                ReservationStatus.OutForDelivery => "Поръчката е вече на път.",
                ReservationStatus.Finished => "Поръчката бе завършена.",
                ReservationStatus.Canceled => "Поръчката бе отказана.",
                _ => "Updated."
            };

            return RedirectToAction(nameof(Index));
        }

        private static bool CanTransition(Reservation reservation, ReservationStatus newStatus)
        {
            // No self-transition
            if (reservation.Status == newStatus)
                return false;

            // Terminal states
            if (reservation.Status is ReservationStatus.Finished or ReservationStatus.Canceled)
                return false;

            // Delivery rule
            if (newStatus == ReservationStatus.OutForDelivery &&
                !string.Equals(reservation.DeliveryType, "Delivery", StringComparison.OrdinalIgnoreCase))
                return false;

            return reservation.Status switch
            {
                ReservationStatus.Pending =>
                    newStatus is ReservationStatus.Confirmed
                    or ReservationStatus.Canceled,

                ReservationStatus.Confirmed =>
                    reservation.DeliveryType.Equals("Delivery", StringComparison.OrdinalIgnoreCase)
                        ? newStatus is ReservationStatus.OutForDelivery
                            or ReservationStatus.Canceled
                        : newStatus is ReservationStatus.Finished
                            or ReservationStatus.Canceled,

                ReservationStatus.OutForDelivery =>
                    newStatus is ReservationStatus.Finished,

                _ => false
            };
        }
    }
}