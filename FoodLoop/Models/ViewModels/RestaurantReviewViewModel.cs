namespace FoodLoop.Models.ViewModels
{
    public class RestaurantReviewViewModel
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string AuthorName { get; set; } = "Anonymous";
        public DateTime CreatedAt { get; set; }
    }
}