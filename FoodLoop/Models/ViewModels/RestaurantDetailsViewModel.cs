using FoodLoop.Models.Entities;

namespace FoodLoop.Models.ViewModels
{
    public class RestaurantDetailsViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = "";
        public string? Address { get; set; }
        public string? ImageUrl { get; set; }
        public string? BannerImageUrl { get; set; }

        public double AvgRating { get; set; }
        public int ReviewsCount { get; set; }

        public int CompletedOrdersCount { get; set; }

        public List<Offer> ActiveOffers { get; set; } = new();
        public List<RestaurantReviewViewModel> LatestReviews { get; set; } = new();
    }
}