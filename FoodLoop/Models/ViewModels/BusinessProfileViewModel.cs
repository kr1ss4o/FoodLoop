namespace FoodLoop.Models.ViewModels
{
    public class BusinessProfileViewModel
    {
        // Business info
        public string RestaurantName { get; set; }
        public string BusinessEmail { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string? ImageUrl { get; set; }
        public string? BannerImageUrl { get; set; }

        // Business profile cards
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int ActiveOffers { get; set; }
        public int TotalReservations { get; set; }

        // Owner info
        public string OwnerName { get; set; }
        public string OwnerEmail { get; set; }
        public string OwnerPhone { get; set; }
        public DateTime AccountCreated { get; set; }

        //Edit modal
        public bool IsRestaurant { get; set; }
        public EditProfileViewModel EditProfileModal { get; set; } = new();
    }
}