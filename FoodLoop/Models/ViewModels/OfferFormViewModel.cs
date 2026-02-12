using FoodLoop.Models.Entities;

namespace FoodLoop.Models.ViewModels
{
    public class OfferFormViewModel
    {
        public Offer Offer { get; set; }
        public List<Category> Categories { get; set; }
        public List<Tag> Tags { get; set; }

        // ✔ Selected tags when creating or editing
        public List<Guid>? SelectedTags { get; set; }
    }
}