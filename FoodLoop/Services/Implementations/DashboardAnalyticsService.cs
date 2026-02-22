using FoodLoop.Data;
using FoodLoop.Models.DTOs;
using FoodLoop.Models.Entities;
using FoodLoop.Models.Enums;
using FoodLoop.Models.ViewModels;
using FoodLoop.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Services.Implementations
{
    public class DashboardAnalyticsService : IDashboardAnalyticsService
    {
        private readonly ApplicationDbContext _context;

        public DashboardAnalyticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RestaurantDashboardViewModel> BuildDashboardAsync(string userId, DateTime? date)
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == userId);

            if (restaurant == null)
                throw new Exception("Restaurant not found.");

            var bulgarianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, bulgarianTimeZone);

            var selectedDate = date?.Date ?? now.Date;
            var prevDate = selectedDate.AddDays(-1);
            var nextDate = selectedDate.AddDays(1);

            var allReservations = await _context.Reservations
            .Include(r => r.User)
            .Include(r => r.Items)
                .ThenInclude(i => i.Offer)
            .Where(r => r.Items.Any(i => i.Offer.RestaurantId == restaurant.Id))
            .ToListAsync();

            var finished = allReservations
                .Where(r => r.Status == ReservationStatus.Finished)
                .ToList();

            // ===== TOTAL =====
            int totalOrders = finished.Count;
            decimal totalRevenue = finished.Sum(r => r.TotalPrice);

            decimal averageOrderValue = totalOrders > 0
                ? totalRevenue / totalOrders
                : 0;

            // ===== WEEK =====
            int diff = (7 + (int)now.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            var weekStart = now.Date.AddDays(-diff);

            var finishedThisWeek = finished
                .Where(r => r.CreatedAt >= weekStart)
                .ToList();

            int ordersThisWeek = finishedThisWeek.Count;
            decimal revenueThisWeek = finishedThisWeek.Sum(r => r.TotalPrice);

            // ===== DAILY TABLE =====
            var dailyReservations = allReservations
            .Where(r => r.CreatedAt.Date == selectedDate)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReservationSummaryDto
            {
                Id = r.Id,
                CreatedAt = r.CreatedAt,
                TotalPrice = r.TotalPrice,
                DeliveryType = r.DeliveryType,
                DeliveryAddress = r.DeliveryAddress,
                Status = r.Status.ToString(),

                CustomerName = r.IsForSomeoneElse
                    ? r.RecipientFullName!
                    : r.User.FullName,

                Phone = r.IsForSomeoneElse
                    ? r.RecipientPhone
                    : r.User.PhoneNumber,

                Items = r.Items.Select(i => new ReservationItemDto
                {
                    OfferTitle = i.Offer.Title,
                    Quantity = i.Quantity
                }).ToList()
            })
            .ToList();

            // ===== DELIVERY / PICKUP =====
            int deliveryOrders = finished.Count(r => r.DeliveryType == "Delivery");
            int pickupOrders = finished.Count(r => r.DeliveryType == "Pickup");

            int pendingOrders = allReservations
                .Count(r => r.Status == ReservationStatus.Pending);

            // ===== TOP OFFER =====
            var topOffer = finished
                .SelectMany(r => r.Items)
                .Where(i => i.Offer.RestaurantId == restaurant.Id)
                .GroupBy(i => new { i.OfferId, i.Offer.Title })
                .Select(g => new { g.Key.Title, Sold = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Sold)
                .FirstOrDefault();

            // ===== REVIEWS (for this restaurant) =====
            var reviews = await _context.Reviews
                .AsNoTracking()
                .Where(rv =>
                    rv.Reservation.Status == ReservationStatus.Finished &&
                    rv.Reservation.Items.Any(i => i.Offer.RestaurantId == restaurant.Id))
                .OrderByDescending(rv => rv.CreatedAt)
                .Select(rv => new FoodLoop.Models.DTOs.RestaurantReviewDto
                {
                    ReviewId = rv.Id,
                    ReservationId = rv.ReservationId,
                    Rating = rv.Rating,
                    Comment = rv.Comment,
                    AuthorName = rv.Reservation.User.FullName,
                    CreatedAt = rv.CreatedAt
                })
                .ToListAsync();

            var totalReviews = reviews.Count;
            var averageRating = totalReviews > 0 ? reviews.Average(x => x.Rating) : 0.0;

            // ===== LOW STOCK =====
            var lowStockOffers = await _context.Offers
                .Where(o => o.RestaurantId == restaurant.Id &&
                            o.QuantityAvailable <= 5)
                .Select(o => new LowStockOfferDto
                {
                    OfferId = o.Id,
                    Title = o.Title,
                    QuantityAvailable = o.QuantityAvailable
                })
                .ToListAsync();

            // ===== CHART (7 days around selected date) =====
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

            return new RestaurantDashboardViewModel
            {
                RestaurantName = restaurant.Name,

                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,

                OrdersThisWeek = ordersThisWeek,
                RevenueThisWeek = revenueThisWeek,

                TopOfferTitle = topOffer?.Title ?? "—",
                TopOfferSoldCount = topOffer?.Sold ?? 0,

                DeliveryOrders = deliveryOrders,
                PickupOrders = pickupOrders,
                PendingOrders = pendingOrders,

                AverageOrderValue = averageOrderValue,

                LowStockOffers = lowStockOffers,

                ChartLabels = labels,
                ChartOrders = ordersSeries,
                ChartRevenue = revenueSeries,

                DailyReservations = dailyReservations,
                SelectedDate = selectedDate,
                PrevDate = prevDate,
                NextDate = nextDate,
                CanGoNext = nextDate <= now.Date,

                //Reviews
                AverageRating = averageRating,
                TotalReviews = totalReviews,
                Reviews = reviews,
            };
        }
    }
}