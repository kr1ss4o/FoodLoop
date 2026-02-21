namespace FoodLoop.Models.ViewModels
{
    public class RestaurantCardViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = "";

        public string? Address { get; set; }
        public string? ImageUrl { get; set; }

        public double AvgRating { get; set; }
        public int ReviewsCount { get; set; }

        public int ActiveOffersCount { get; set; }
    }
}