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

        public RestaurantDashboardController(
            ApplicationDbContext context,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

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
                return RedirectToAction("Index", "Home");
            }

            // Всички поръчки за този ресторант (през ReservationItems)
            var reservationsQuery = _context.Reservations
                .AsNoTracking()
                .Include(r => r.Items)
                    .ThenInclude(i => i.Offer)
                .Where(r => r.Items.Any(i => i.Offer.RestaurantId == restaurant.Id));

            var allReservations = await reservationsQuery.ToListAsync();

            var finished = allReservations
                .Where(r => r.Status == ReservationStatus.Finished)
                .ToList();

            var now = DateTime.UtcNow;

            // ==========================
            // KPI: TOTAL
            // ==========================
            int totalOrders = finished.Count;
            decimal totalRevenue = finished.Sum(r => r.TotalPrice);

            decimal averageOrderValue = totalOrders > 0
                ? totalRevenue / totalOrders
                : 0;

            // ==========================
            // KPI: THIS WEEK
            // ==========================
            int diff = (7 + (int)now.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            var weekStart = now.Date.AddDays(-diff);

            var finishedThisWeek = finished
                .Where(r => r.CreatedAt >= weekStart)
                .ToList();

            int ordersThisWeek = finishedThisWeek.Count;
            decimal revenueThisWeek = finishedThisWeek.Sum(r => r.TotalPrice);

            // ==========================
            // KPI: DELIVERY / PICKUP / GIFT
            // ==========================
            int deliveryOrders = finished.Count(r => r.DeliveryType == "Delivery");
            int pickupOrders = finished.Count(r => r.DeliveryType == "Pickup");
            int ordersForOthers = allReservations
            .Count(r => r.IsForSomeoneElse && r.Status != ReservationStatus.Canceled);


            int pendingOrders = allReservations
                .Count(r => r.Status == ReservationStatus.Pending);

            // ==========================
            // TOP OFFER
            // ==========================
            var topOffer = finished
                .SelectMany(r => r.Items)
                .Where(i => i.Offer.RestaurantId == restaurant.Id)
                .GroupBy(i => new { i.OfferId, i.Offer.Title })
                .Select(g => new
                {
                    g.Key.Title,
                    Sold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Sold)
                .FirstOrDefault();

            // ==========================
            // LOW STOCK
            // ==========================
            var lowStockOffers = await _context.Offers
                .AsNoTracking()
                .Where(o => o.RestaurantId == restaurant.Id &&
                            o.QuantityAvailable <= 5)
                .OrderBy(o => o.QuantityAvailable)
                .Select(o => new LowStockOfferDto
                {
                    OfferId = o.Id,
                    Title = o.Title,
                    QuantityAvailable = o.QuantityAvailable
                })
                .ToListAsync();

            // ==========================
            // CHART (Last 7 days)
            // ==========================
            var labels = new List<string>();
            var ordersSeries = new List<int>();
            var revenueSeries = new List<decimal>();

            for (int i = 6; i >= 0; i--)
            {
                var day = now.Date.AddDays(-i);
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

                TopOfferTitle = topOffer?.Title ?? "—",
                TopOfferSoldCount = topOffer?.Sold ?? 0,

                LowStockOffers = lowStockOffers,

                ChartLabels = labels,
                ChartOrders = ordersSeries,
                ChartRevenue = revenueSeries,

                DeliveryOrders = deliveryOrders,
                PickupOrders = pickupOrders,
                OrdersForOthers = ordersForOthers,
                PendingOrders = pendingOrders,
                AverageOrderValue = averageOrderValue
            };

            return View(vm);
        }
    }
}