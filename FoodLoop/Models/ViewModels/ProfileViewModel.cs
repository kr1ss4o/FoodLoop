namespace FoodLoop.Models.ViewModels
{
    public class ProfileViewModel
    {
        // Basic user info
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? ProfileImageUrl { get; set; }

        // Insights
        public int TotalOrders { get; set; }
        public decimal MoneySpent { get; set; }
        public DateTime AccountCreated { get; set; }
    }
}