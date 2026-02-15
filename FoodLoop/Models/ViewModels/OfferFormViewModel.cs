using FoodLoop.Models.Entities;

namespace FoodLoop.Models.ViewModels
{
    public class OfferFormViewModel
    {
        public Offer Offer { get; set; } = new();

        public List<Category>? Categories { get; set; }
        public List<Tag>? Tags { get; set; }

        public List<Guid>? SelectedTags { get; set; }
    }
}