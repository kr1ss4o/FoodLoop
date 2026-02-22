namespace FoodLoop.Models.DTOs
{
    public class RestaurantReviewDto
    {
        public Guid ReviewId { get; set; }
        public Guid ReservationId { get; set; }

        public int Rating { get; set; }
        public string? Comment { get; set; }

        public string AuthorName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}