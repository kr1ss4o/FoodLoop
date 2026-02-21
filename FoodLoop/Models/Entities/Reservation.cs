using FoodLoop.Models.Enums;

namespace FoodLoop.Models.Entities
{
    public class Reservation : BaseEntity
    {
        public string UserId { get; set; } = null!;
        public User User { get; set; } = null!;
        public ReservationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string DeliveryType { get; set; } = "Pickup";
        public string? DeliveryAddress { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsForSomeoneElse { get; set; }
        public string? RecipientFullName { get; set; }
        public string? RecipientPhone { get; set; }
        public Review? Review { get; set; }
        public ICollection<ReservationStatusLog> StatusLogs { get; set; } = new List<ReservationStatusLog>();
        public ICollection<ReservationItem> Items { get; set; } = new List<ReservationItem>();
    }

}