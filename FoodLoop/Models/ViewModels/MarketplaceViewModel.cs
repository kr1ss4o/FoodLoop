using FoodLoop.Models.Entities;

namespace FoodLoop.Models.ViewModels
{
    public class MarketplaceViewModel
    {
        public List<Offer> BreakfastOffers { get; set; } = new();
        public List<Offer> LunchOffers { get; set; } = new();
        public List<Offer> DinnerOffers { get; set; } = new();

        public int BreakfastPage { get; set; }
        public int LunchPage { get; set; }
        public int DinnerPage { get; set; }

        public int BreakfastTotalPages { get; set; }
        public int LunchTotalPages { get; set; }
        public int DinnerTotalPages { get; set; }
    }
}