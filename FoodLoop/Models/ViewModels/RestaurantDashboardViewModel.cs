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

        public decimal AverageOrderValue { get; set; }


        public List<LowStockOfferDto> LowStockOffers { get; set; } = new();

        // Chart (последни 7 дни)
        public List<string> ChartLabels { get; set; } = new();
        public List<int> ChartOrders { get; set; } = new();
        public List<decimal> ChartRevenue { get; set; } = new();
    }

    public class LowStockOfferDto
    {
        public Guid OfferId { get; set; }
        public string Title { get; set; } = "";
        public int QuantityAvailable { get; set; }
    }
}