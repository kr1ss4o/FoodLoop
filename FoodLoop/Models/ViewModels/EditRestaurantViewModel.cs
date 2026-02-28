namespace FoodLoop.Models.ViewModels.Admin
{
    public class EditRestaurantViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
        public string BusinessEmail { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}