using FoodLoop.Models.Entities;

namespace FoodLoop.Models.ViewModels
{
    public class SearchViewModel
    {
        public string? Query { get; set; }

        public List<Offer> Offers { get; set; } = new();
        public List<Restaurant> Restaurants { get; set; } = new();

        // Pagination за офертите
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        // Pagination за ресторантите
        public int RestaurantPages { get; set; }

        public string? Sort { get; set; }
        public Guid? CategoryId { get; set; }

        // Pagination ресторанти в търсачката
        public int RestaurantsCurrentPage { get; set; }

        // Общ брой резултати
        public int OffersCount { get; set; }
        public int RestaurantsCount { get; set; }
    }
}