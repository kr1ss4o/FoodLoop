using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FoodLoop.Models.Entities
{
    public class User : IdentityUser
    {
        [Display(Name = "Пълно име")]
        public string FullName { get; set; }

        [Display(Name = "Имейл")]
        public override string? Email { get; set; }

        [Display(Name = "Телефон")]
        public override string? PhoneNumber { get; set; }

        [Display(Name = "Регистриран на")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? ProfileImageUrl { get; set; }

        // Navigation
        public ICollection<Restaurant>? RestaurantsOwned { get; set; }
        public ICollection<Reservation>? Reservations { get; set; }
    }
}