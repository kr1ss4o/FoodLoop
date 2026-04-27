using System.ComponentModel.DataAnnotations;

namespace FoodLoop.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        public string FullName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [StringLength(10, MinimumLength = 10, ErrorMessage = "Телефонът трябва да е точно 10 цифри.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Телефонът трябва да съдържа само цифри.")]
        public string? Phone { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; }

        [Required, DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}