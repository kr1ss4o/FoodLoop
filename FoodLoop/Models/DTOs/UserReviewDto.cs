namespace FoodLoop.Models.DTOs
{
    public class UserReviewDto
    {
        public Guid ReviewId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string RestaurantName { get; set; } = "";
    }
}
