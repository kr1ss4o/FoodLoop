namespace FoodLoop.Models.Entities
{
    public class Offer : BaseEntity
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";

        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public int QuantityAvailable { get; set; }

        public DateTime EndsAt { get; set; }
        public string? ImageUrl { get; set; }

        // FK to Restaurant
        public Guid RestaurantId { get; set; }
        public Restaurant? Restaurant { get; set; }   // ✅ nullable

        // FK to Category
        public Guid CategoryId { get; set; }
        public Category? Category { get; set; }       // ✅ nullable

        // Many-to-Many
        public ICollection<OfferTag>? OfferTags { get; set; } = new List<OfferTag>();

        public ICollection<Reservation>? Reservations { get; set; }
    }
}