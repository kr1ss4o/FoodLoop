using FoodLoop.Models.Entities;

namespace FoodLoop.Models.ViewModels
{
    public class MarketplaceViewModel
    {
        public List<Offer> Offers { get; set; } = new();

        public string? Sort { get; set; }
    }
}