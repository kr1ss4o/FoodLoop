using FoodLoop.Models.Entities;

namespace FoodLoop.ViewModels.Admin;

public class AdminFormViewModel
{
    public string Type { get; set; } = "";

    public Guid? Id { get; set; }

    // Restaurant

    public string? OwnerEmail { get; set; }
    public string? OwnerPassword { get; set; }
    public string? OwnerFullName { get; set; }

    public string? RestaurantName { get; set; }
    public string? BusinessEmail { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? ImageUrl { get; set; }

    // Offer

    public string? Title { get; set; }
    public string? Description { get; set; }

    public decimal? OriginalPrice { get; set; }
    public decimal? DiscountedPrice { get; set; }

    public int? QuantityAvailable { get; set; }

    public DateTime? EndsAt { get; set; }

    public Guid? RestaurantId { get; set; }
    public Guid? CategoryId { get; set; }

    public List<Guid>? Tags { get; set; }

    // dropdowns

    public List<Restaurant>? Restaurants { get; set; }
    public List<Category>? Categories { get; set; }
    public List<Tag>? AllTags { get; set; }
}