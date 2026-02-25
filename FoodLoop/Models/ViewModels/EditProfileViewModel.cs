public class EditProfileViewModel
{
    // Common
    public string FullName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string? ProfileImageUrl { get; set; }
    public string? BannerImageUrl { get; set; }

    public string CurrentPassword { get; set; } = "";
    public string? NewPassword { get; set; }

    // Restaurant only
    public string? RestaurantName { get; set; }
    public string? BusinessEmail { get; set; }
    public string? Address { get; set; }

    public bool IsRestaurant { get; set; }

    // Business profile display
}