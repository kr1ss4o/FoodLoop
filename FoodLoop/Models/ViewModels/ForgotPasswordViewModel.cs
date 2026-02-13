using System.ComponentModel.DataAnnotations;

namespace FoodLoop.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }
    }
}