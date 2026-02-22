namespace FoodLoop.Models.DTOs
{
    public class ReservationSummaryDto
    {
        public Guid Id { get; set; }

        public string CustomerName { get; set; } = "";
        public string? Phone { get; set; }

        public string DeliveryType { get; set; } = "";
        public string? DeliveryAddress { get; set; }

        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        public List<ReservationItemDto> Items { get; set; } = new();
    }

    public class ReservationItemDto
    {
        public string OfferTitle { get; set; } = "";
        public int Quantity { get; set; }
    }
}