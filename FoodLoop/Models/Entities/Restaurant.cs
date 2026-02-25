namespace FoodLoop.Models.Entities
{
    public class Restaurant : BaseEntity
    {
        public string Name { get; set; }
        public string BusinessEmail { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string? ImageUrl { get; set; }
        public string? BannerImageUrl { get; set; }
        public double Rating { get; set; }

        // FK to Owner (User)
        public string OwnerUserId { get; set; }
        public User Owner { get; set; }

        // Navigation
        public ICollection<Offer>? Offers { get; set; }
    }
}