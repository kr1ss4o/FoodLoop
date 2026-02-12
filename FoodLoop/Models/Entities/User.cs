using Microsoft.AspNetCore.Identity;

namespace FoodLoop.Models.Entities
{
    public class User : IdentityUser
    {
        public string FullName { get; set; }
        public string? ProfileImageUrl { get; set; }

        // Role will be managed through Identity roles:
        // "Client", "Business", "Admin"

        // Navigation
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Restaurant>? RestaurantsOwned { get; set; }
        public ICollection<Reservation>? Reservations { get; set; }
    }
}