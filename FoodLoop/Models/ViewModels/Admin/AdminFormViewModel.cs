using FoodLoop.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace FoodLoop.ViewModels.Admin;

public class AdminFormViewModel
{
    public string Type { get; set; } = "";

    public Guid? Id { get; set; }

    // =========================
    // OWNER
    // =========================

    public string? OwnerEmail { get; set; }

    [RegularExpression(@"^\d{10}$",
    ErrorMessage = "Телефонният номер трябва да съдържа точно 10 цифри.")]
    public string? OwnerPhone { get; set; }
    public string? OwnerPassword { get; set; }
    public string? OwnerFullName { get; set; }

    // =========================
    // RESTAURANT
    // =========================

    public string? RestaurantName { get; set; }
    public string? BusinessEmail { get; set; }

    [RegularExpression(@"^\d{10}$",
    ErrorMessage = "Телефонният номер трябва да съдържа точно 10 цифри.")]
    public string? Phone { get; set; }
    public string? Address { get; set; }

    public string? ImageUrl { get; set; }
    public string? BannerImageUrl { get; set; }

    // =========================
    // OFFER
    // =========================

    public string? Title { get; set; }
    public string? Description { get; set; }

    public decimal? OriginalPrice { get; set; }
    public decimal? DiscountedPrice { get; set; }

    public int? QuantityAvailable { get; set; }

    public DateTime? EndsAt { get; set; }

    public Guid? RestaurantId { get; set; }
    public Guid? CategoryId { get; set; }

    public List<Guid>? Tags { get; set; }

    // =====================
    // CATEGORY / TAG
    // =====================

    public string? Name { get; set; }
    public string? Icon { get; set; }

    // =========================
    // DROPDOWNS
    // =========================

    public List<Restaurant>? Restaurants { get; set; }
    public List<Category>? Categories { get; set; }
    public List<Tag>? AllTags { get; set; }
}