namespace FoodLoop.Models.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; set; }
        public string? IconUrl { get; set; }

        public ICollection<Offer>? Offers { get; set; }
    }
}