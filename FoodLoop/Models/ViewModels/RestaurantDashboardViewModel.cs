using FoodLoop.Models.Entities;
using FoodLoop.Models.DTOs;

namespace FoodLoop.Models.ViewModels
{
    public class RestaurantDashboardViewModel
    {
        public string RestaurantName { get; set; } = "";

        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        public int OrdersThisWeek { get; set; }
        public decimal RevenueThisWeek { get; set; }
        public string TopOfferTitle { get; set; } = "—";
        public int TopOfferSoldCount { get; set; }
        public int DeliveryOrders { get; set; }
        public int PickupOrders { get; set; }
        public int OrdersForOthers { get; set; }
        public int PendingOrders { get; set; }
        public List<ReservationSummaryDto> DailyReservations { get; set; } = new();
        public DateTime SelectedDate { get; set; }
        public decimal AverageOrderValue { get; set; }
        public DateTime PrevDate { get; set; }
        public DateTime NextDate { get; set; }
        public bool CanGoNext { get; set; }

        // Rating related fields
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<RestaurantReviewDto> Reviews { get; set; } = new();
        // Rating distribution
        public Dictionary<int, int> RatingCounts { get; set; } = new();
        public Dictionary<int, double> RatingPercentages { get; set; } = new();

        // Cancel rate
        public double CancellationRate { get; set; }

        // Peak hours
        public List<string> HourLabels { get; set; } = new();
        public List<int> HourOrders { get; set; } = new();

        public List<LowStockOfferDto> LowStockOffers { get; set; } = new();

        // Chart (последни 7 дни)
        public List<string> ChartLabels { get; set; } = new();
        public List<int> ChartOrders { get; set; } = new();
        public List<decimal> ChartRevenue { get; set; } = new();
        
        // Monthly KPIs + Heatmap days
        public List<HeatmapDayDto> HeatmapDays { get; set; } = new();
        public int OrdersThisMonth { get; set; }
        public decimal RevenueThisMonth { get; set; }
    }
    public class LowStockOfferDto
    {
        public Guid OfferId { get; set; }
        public string Title { get; set; } = "";
        public int QuantityAvailable { get; set; }
    }
    public class HeatmapDayDto
    {
        public DateTime Date { get; set; }
        public int OrdersCount { get; set; }
    }
}