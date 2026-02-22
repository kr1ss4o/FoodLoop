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
    [Authorize(Roles = "Restaurant")]
    public class RestaurantDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public RestaurantDashboardController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(DateTime? date)
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
                return RedirectToAction("Index", "Offers");
            }

            // Избран ден (UTC дата)
            var selectedDate = (date?.Date ?? DateTime.UtcNow.Date);

            // За UI навигация
            var prevDate = selectedDate.AddDays(-1);
            var nextDate = selectedDate.AddDays(1);
            bool canGoNext = nextDate <= DateTime.UtcNow.Date;

            // Всички резервации за този ресторант (минава през items -> offer -> restaurantId)
            var allReservations = await _context.Reservations
                .AsNoTracking()
                .Include(r => r.Items)
                    .ThenInclude(i => i.Offer)
                .Where(r => r.Items.Any(i => i.Offer.RestaurantId == restaurant.Id))
                .ToListAsync();

            var finished = allReservations
                .Where(r => r.Status == ReservationStatus.Finished)
                .ToList();

            // ==========================
            // KPI: TOTAL (all-time finished)
            // ==========================
            int totalOrders = finished.Count;
            decimal totalRevenue = finished.Sum(r => r.TotalPrice);

            decimal averageOrderValue = totalOrders > 0
                ? totalRevenue / totalOrders
                : 0m;

            // ==========================
            // KPI: THIS WEEK (Mon..Now, finished only)
            // ==========================
            var now = DateTime.UtcNow;
            int diff = (7 + (int)now.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            var weekStart = now.Date.AddDays(-diff);

            var finishedThisWeek = finished
                .Where(r => r.CreatedAt >= weekStart)
                .ToList();

            int ordersThisWeek = finishedThisWeek.Count;
            decimal revenueThisWeek = finishedThisWeek.Sum(r => r.TotalPrice);

            // ==========================
            // KPI: statuses / types
            // ==========================
            int pendingOrders = allReservations.Count(r => r.Status == ReservationStatus.Pending);

            int deliveryOrders = finished.Count(r => r.DeliveryType == "Delivery");
            int pickupOrders = finished.Count(r => r.DeliveryType == "Pickup");

            int ordersForOthers = allReservations.Count(r =>
                r.IsForSomeoneElse && r.Status != ReservationStatus.Canceled);

            // =========================
            // KPI: RATING AND REVIEWS
            // =========================

            var reviewsQuery = _context.Reviews
            .Where(r =>
                r.Reservation.Items
                    .Any(i => i.Offer.RestaurantId == restaurant.Id));

            double avgRating = await reviewsQuery.AnyAsync()
                ? await reviewsQuery.AverageAsync(r => r.Rating) : 0;

            int totalReviews = await reviewsQuery.CountAsync();

            // ==========================
            // TOP OFFER (by sold qty)
            // ==========================
            var topOffer = finished
                .SelectMany(r => r.Items)
                .Where(i => i.Offer.RestaurantId == restaurant.Id)
                .GroupBy(i => new { i.OfferId, i.Offer.Title })
                .Select(g => new { g.Key.Title, Sold = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Sold)
                .FirstOrDefault();

            // ==========================
            // DAILY RESERVATIONS (selected day) - за таблица/лист, ако решиш да го покажеш
            // ==========================
            var dayStart = selectedDate;
            var dayEnd = selectedDate.AddDays(1);

            var dailyReservations = await _context.Reservations
                .AsNoTracking()
                .Include(r => r.Items)
                    .ThenInclude(i => i.Offer)
                .Include(r => r.User)
                .Include(r => r.StatusLogs)
                .Where(r =>
                    r.Items.Any(i => i.Offer.RestaurantId == restaurant.Id) &&
                    r.CreatedAt >= dayStart && r.CreatedAt < dayEnd)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // ==========================
            // CHART: movable 7-day window ending at selectedDate
            // (selectedDate - 6 .. selectedDate)
            // ==========================
            var labels = new List<string>();
            var ordersSeries = new List<int>();
            var revenueSeries = new List<decimal>();

            for (int i = 6; i >= 0; i--)
            {
                var day = selectedDate.AddDays(-i);
                var next = day.AddDays(1);

                labels.Add(day.ToString("dd MMM"));

                var dayFinished = finished
                    .Where(r => r.CreatedAt >= day && r.CreatedAt < next)
                    .ToList();

                ordersSeries.Add(dayFinished.Count);
                revenueSeries.Add(dayFinished.Sum(r => r.TotalPrice));
            }

            var vm = new RestaurantDashboardViewModel
            {
                RestaurantName = restaurant.Name,

                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,

                OrdersThisWeek = ordersThisWeek,
                RevenueThisWeek = revenueThisWeek,

                AverageOrderValue = averageOrderValue,

                PendingOrders = pendingOrders,
                DeliveryOrders = deliveryOrders,
                PickupOrders = pickupOrders,
                OrdersForOthers = ordersForOthers,

                TopOfferTitle = topOffer?.Title ?? "—",
                TopOfferSoldCount = topOffer?.Sold ?? 0,

                AverageRating = avgRating,
                TotalReviews = totalReviews,

                ChartLabels = labels,
                ChartOrders = ordersSeries,
                ChartRevenue = revenueSeries,

                DailyReservations = dailyReservations,
                SelectedDate = selectedDate
            };

            vm.PrevDate = prevDate;
            vm.NextDate = nextDate;
            vm.CanGoNext = canGoNext;

            return View(vm);
        }
    }
}