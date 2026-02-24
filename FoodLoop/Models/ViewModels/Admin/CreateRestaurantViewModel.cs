using System.ComponentModel.DataAnnotations;

namespace FoodLoop.Models.ViewModels.Admin
{
    public class CreateRestaurantViewModel
    {
        // Account (Identity User)
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, MinLength(6)]
        public string Password { get; set; } = "";

        [Required]
        public string OwnerFullName { get; set; } = "";

        // Restaurant entity fields
        [Required]
        public string RestaurantName { get; set; } = "";

        [Required, EmailAddress]
        public string BusinessEmail { get; set; } = "";

        [Required]
        public string Phone { get; set; } = "";

        [Required]
        public string Address { get; set; } = "";

        public string? ImageUrl { get; set; }
    }
}