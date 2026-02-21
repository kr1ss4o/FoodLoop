using FoodLoop.Models.Enums;

namespace FoodLoop.Models.Entities
{
    public class ReservationStatusLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ReservationId { get; set; }
        public Reservation Reservation { get; set; } = null!;

        public ReservationStatus OldStatus { get; set; }
        public ReservationStatus NewStatus { get; set; }

        public string ChangedByUserId { get; set; } = null!;
        public User ChangedByUser { get; set; } = null!;

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}