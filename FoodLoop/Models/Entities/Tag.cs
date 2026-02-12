namespace FoodLoop.Models.Entities
{
    public class Tag : BaseEntity
    {
        public string Name { get; set; }

        public ICollection<OfferTag>? OfferTags { get; set; }
    }
}