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
                .Where(r => r.OwnerUserId == userId)
                .Select(r => new { r.Id, r.Name })
                .FirstOrDefaultAsync();

            if (restaurant == null)
                throw new Exception("Restaurant not found.");

            var tz = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

            var selectedDate = date?.Date ?? now.Date;
            var prevDate = selectedDate.AddDays(-1);
            var nextDate = selectedDate.AddDays(1);

            var reservationsQuery = _context.Reservations
                .Where(r => r.Items.Any(i => i.Offer.RestaurantId == restaurant.Id));

            var finishedQuery = reservationsQuery
                .Where(r => r.Status == ReservationStatus.Finished);

            // ================= TOTAL =================

            int totalOrders = await finishedQuery.CountAsync();
            decimal totalRevenue = totalOrders > 0
                ? await finishedQuery.SumAsync(r => r.TotalPrice)
                : 0;

            decimal averageOrderValue = totalOrders > 0
                ? totalRevenue / totalOrders
                : 0;

            // ================= CANCELLATION RATE =================

            int totalReservations = await reservationsQuery.CountAsync();
            int canceledOrders = await reservationsQuery
                .CountAsync(r => r.Status == ReservationStatus.Canceled);

            double cancellationRate = totalReservations > 0
                ? (double)canceledOrders / totalReservations * 100
                : 0;

            // ================= WEEK =================

            int diff = (7 + (int)now.DayOfWeek - (int)DayOfWeek.Monday) % 7;
            var weekStartLocal = now.Date.AddDays(-diff);
            var weekStartUtc = TimeZoneInfo.ConvertTimeToUtc(weekStartLocal, tz);

            int ordersThisWeek = await finishedQuery
                .CountAsync(r => r.CreatedAt >= weekStartUtc);

            decimal revenueThisWeek = ordersThisWeek > 0
                ? await finishedQuery
                    .Where(r => r.CreatedAt >= weekStartUtc)
                    .SumAsync(r => r.TotalPrice)
                : 0;

            // ================= MONTH =================

            var monthStartLocal = new DateTime(now.Year, now.Month, 1);
            var monthStartUtc = TimeZoneInfo.ConvertTimeToUtc(monthStartLocal, tz);

            int ordersThisMonth = await finishedQuery
                .CountAsync(r => r.CreatedAt >= monthStartUtc);

            decimal revenueThisMonth = ordersThisMonth > 0
                ? await finishedQuery
                    .Where(r => r.CreatedAt >= monthStartUtc)
                    .SumAsync(r => r.TotalPrice)
                : 0;

            // ================= DELIVERY / PICKUP =================

            int deliveryOrders = await finishedQuery
                .CountAsync(r => r.DeliveryType == "Delivery");

            int pickupOrders = await finishedQuery
                .CountAsync(r => r.DeliveryType == "Pickup");

            int pendingOrders = await reservationsQuery
                .CountAsync(r => r.Status == ReservationStatus.Pending);

            // ================= DAILY TABLE =================

            var selectedStartUtc = TimeZoneInfo.ConvertTimeToUtc(selectedDate, tz);
            var selectedEndUtc = TimeZoneInfo.ConvertTimeToUtc(selectedDate.AddDays(1), tz);

            var dailyReservations = await reservationsQuery
                .Where(r => r.CreatedAt >= selectedStartUtc && r.CreatedAt < selectedEndUtc)
                .Include(r => r.User)
                .Include(r => r.Items)
                    .ThenInclude(i => i.Offer)
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
                .ToListAsync();

            // ================= TOP OFFER =================

            var topOffer = await _context.ReservationItems
                .Where(ri =>
                    ri.Reservation.Status == ReservationStatus.Finished &&
                    ri.Offer.RestaurantId == restaurant.Id)
                .GroupBy(ri => new { ri.OfferId, ri.Offer.Title })
                .Select(g => new
                {
                    g.Key.Title,
                    Sold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Sold)
                .FirstOrDefaultAsync();

            // ================= HEATMAP (Last 30 days) =================

            var heatmapStartLocal = now.Date.AddDays(-29);
            var heatmapStartUtc = TimeZoneInfo.ConvertTimeToUtc(heatmapStartLocal, tz);
            var heatmapEndUtc = TimeZoneInfo.ConvertTimeToUtc(now.Date.AddDays(1), tz);

            var heatmapData = await reservationsQuery
                .Where(r => r.CreatedAt >= heatmapStartUtc && r.CreatedAt < heatmapEndUtc)
                .Select(r => r.CreatedAt)
                .ToListAsync();

            var heatmapCounts = heatmapData
                .GroupBy(d => TimeZoneInfo.ConvertTimeFromUtc(d, tz).Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var heatmapDays = new List<HeatmapDayDto>();

            for (int i = 29; i >= 0; i--)
            {
                var dayLocal = now.Date.AddDays(-i);

                heatmapDays.Add(new HeatmapDayDto
                {
                    Date = dayLocal,
                    OrdersCount = heatmapCounts.TryGetValue(dayLocal, out var c) ? c : 0
                });
            }

            // ================= CHART =================

            var chartStartLocal = selectedDate.AddDays(-6);
            var chartStartUtc = TimeZoneInfo.ConvertTimeToUtc(chartStartLocal, tz);

            var chartData = await finishedQuery
                .Where(r => r.CreatedAt >= chartStartUtc && r.CreatedAt < selectedEndUtc)
                .GroupBy(r => r.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(x => x.TotalPrice)
                })
                .ToListAsync();

            var chartDict = chartData.ToDictionary(x => x.Date);

            var labels = new List<string>();
            var ordersSeries = new List<int>();
            var revenueSeries = new List<decimal>();

            for (int i = 0; i < 7; i++)
            {
                var day = chartStartLocal.AddDays(i);

                labels.Add(day.ToString("dd MMM"));

                if (chartDict.TryGetValue(day, out var data))
                {
                    ordersSeries.Add(data.Count);
                    revenueSeries.Add(data.Revenue);
                }
                else
                {
                    ordersSeries.Add(0);
                    revenueSeries.Add(0);
                }
            }

            // ================= REVIEWS =================

            var reviewsQuery = _context.Reviews
                .Where(rv =>
                    rv.Reservation.Status == ReservationStatus.Finished &&
                    rv.Reservation.Items.Any(i => i.Offer.RestaurantId == restaurant.Id));

            int totalReviews = await reviewsQuery.CountAsync();
            double averageRating = totalReviews > 0
                ? await reviewsQuery.AverageAsync(r => r.Rating)
                : 0;

            var reviews = await reviewsQuery
                .OrderByDescending(rv => rv.CreatedAt)
                .Take(10)
                .Select(rv => new RestaurantReviewDto
                {
                    ReviewId = rv.Id,
                    ReservationId = rv.ReservationId,
                    Rating = rv.Rating,
                    Comment = rv.Comment,
                    AuthorName = rv.Reservation.User.FullName,
                    CreatedAt = rv.CreatedAt
                })
                .ToListAsync();

            // ================= PEAK HOURS (Last 30 days, Finished) =================
            var peakStartLocal = now.Date.AddDays(-29);
            var peakEndLocalExclusive = now.Date.AddDays(1);

            var peakStartUtc = TimeZoneInfo.ConvertTimeToUtc(peakStartLocal, tz);
            var peakEndUtc = TimeZoneInfo.ConvertTimeToUtc(peakEndLocalExclusive, tz);

            // взима само датите (лекичко) и групира в паметта по локален час
            var peakDatesUtc = await finishedQuery
                .Where(r => r.CreatedAt >= peakStartUtc && r.CreatedAt < peakEndUtc)
                .Select(r => r.CreatedAt)
                .ToListAsync();

            var hourCounts = peakDatesUtc
                .Select(d => TimeZoneInfo.ConvertTimeFromUtc(d, tz).Hour)
                .GroupBy(h => h)
                .ToDictionary(g => g.Key, g => g.Count());

            // 00..23
            var hourLabels = Enumerable.Range(0, 24).Select(h => $"{h:00}:00 ч.").ToList();
            var hourOrders = Enumerable.Range(0, 24).Select(h => hourCounts.TryGetValue(h, out var c) ? c : 0).ToList();

            return new RestaurantDashboardViewModel
            {
                RestaurantName = restaurant.Name,

                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                AverageOrderValue = averageOrderValue,

                OrdersThisWeek = ordersThisWeek,
                RevenueThisWeek = revenueThisWeek,

                OrdersThisMonth = ordersThisMonth,
                RevenueThisMonth = revenueThisMonth,

                DeliveryOrders = deliveryOrders,
                PickupOrders = pickupOrders,
                PendingOrders = pendingOrders,

                CancellationRate = Math.Round(cancellationRate, 1),

                TopOfferTitle = topOffer?.Title ?? "—",
                TopOfferSoldCount = topOffer?.Sold ?? 0,

                ChartLabels = labels,
                ChartOrders = ordersSeries,
                ChartRevenue = revenueSeries,

                DailyReservations = dailyReservations,
                HeatmapDays = heatmapDays,

                HourLabels = hourLabels,
                HourOrders = hourOrders,

                SelectedDate = selectedDate,
                PrevDate = prevDate,
                NextDate = nextDate,
                CanGoNext = nextDate <= now.Date,

                AverageRating = averageRating,
                TotalReviews = totalReviews,
                Reviews = reviews
            };
        }
    }
}