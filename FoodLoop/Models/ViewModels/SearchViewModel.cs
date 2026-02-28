using FoodLoop.Models.Entities;

namespace FoodLoop.Models.ViewModels
{
    public class SearchViewModel
    {
        public string? Query { get; set; }

        public List<Offer> Offers { get; set; } = new();
        public List<Restaurant> Restaurants { get; set; } = new();

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public string? Sort { get; set; }
        public Guid? CategoryId { get; set; }
    }
}