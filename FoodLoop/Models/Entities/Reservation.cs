using FoodLoop.Models.Enums;

namespace FoodLoop.Models.Entities
{
    public class Reservation
    {
        public Guid Id { get; set; }

        public Guid OfferId { get; set; }
        public Offer Offer { get; set; }

        public string UserId { get; set; }
        public User User { get; set; }
        public int Quantity { get; set; } = 1;


        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}