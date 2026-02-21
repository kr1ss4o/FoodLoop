using System.ComponentModel.DataAnnotations;

namespace FoodLoop.Models.Entities
{
    public class Review
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // 1:1 връзка с Reservation
        [Required]
        public Guid ReservationId { get; set; }

        public Reservation Reservation { get; set; } = null!;

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}