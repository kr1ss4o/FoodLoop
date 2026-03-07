using FoodLoop.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace FoodLoop.ViewModels.Admin;

public class AdminFormViewModel : IValidatableObject
{
    public string Type { get; set; } = "";

    public Guid? Id { get; set; }

    // =========================
    // OWNER
    // =========================

    [EmailAddress(ErrorMessage = "Невалиден имейл")]
    public string? OwnerEmail { get; set; }

    [RegularExpression(@"^\d{10}$",
        ErrorMessage = "Телефонният номер трябва да съдържа точно 10 цифри")]
    public string? OwnerPhone { get; set; }

    [MinLength(6, ErrorMessage = "Паролата трябва да е поне 6 символа")]
    public string? OwnerPassword { get; set; }

    public string? OwnerFullName { get; set; }

    // =========================
    // RESTAURANT
    // =========================

    [EmailAddress(ErrorMessage = "Невалиден имейл")]
    public string? BusinessEmail { get; set; }

    [RegularExpression(@"^\d{10}$",
        ErrorMessage = "Телефонният номер трябва да съдържа точно 10 цифри")]
    public string? Phone { get; set; }

    public string? RestaurantName { get; set; }

    public string? Address { get; set; }

    public string? ImageUrl { get; set; }
    public string? BannerImageUrl { get; set; }

    // =========================
    // OFFER
    // =========================

    public string? Title { get; set; }

    public string? Description { get; set; }

    [Range(0.01, 10000, ErrorMessage = "Невалидна цена")]
    public decimal? OriginalPrice { get; set; }

    [Range(0.01, 10000, ErrorMessage = "Невалидна цена")]
    public decimal? DiscountedPrice { get; set; }

    [Range(1, 1000, ErrorMessage = "Невалидно количество")]
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

    // =========================
    // CONDITIONAL VALIDATION
    // =========================

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type == "Restaurant")
        {
            if (string.IsNullOrWhiteSpace(RestaurantName))
                yield return new ValidationResult("Името на ресторанта е задължително", new[] { nameof(RestaurantName) });

            if (string.IsNullOrWhiteSpace(BusinessEmail))
                yield return new ValidationResult("Бизнес имейлът е задължителен", new[] { nameof(BusinessEmail) });

            if (string.IsNullOrWhiteSpace(Address))
                yield return new ValidationResult("Адресът е задължителен", new[] { nameof(Address) });

            if (string.IsNullOrWhiteSpace(Phone))
                yield return new ValidationResult("Телефонът е задължителен", new[] { nameof(Phone) });

            if (string.IsNullOrWhiteSpace(OwnerFullName))
                yield return new ValidationResult("Името на собственика е задължително", new[] { nameof(OwnerFullName) });

            if (string.IsNullOrWhiteSpace(OwnerEmail))
                yield return new ValidationResult("Имейлът е задължителен", new[] { nameof(OwnerEmail) });

            if (Id == null && string.IsNullOrWhiteSpace(OwnerPassword))
                yield return new ValidationResult("Паролата е задължителна", new[] { nameof(OwnerPassword) });
        }

        if (Type == "Offer")
        {
            if (string.IsNullOrWhiteSpace(Title))
                yield return new ValidationResult("Заглавието е задължително", new[] { nameof(Title) });

            if (OriginalPrice == null)
                yield return new ValidationResult("Оригиналната цена е задължителна", new[] { nameof(OriginalPrice) });

            if (DiscountedPrice == null)
                yield return new ValidationResult("Цената е задължителна", new[] { nameof(DiscountedPrice) });

            if (QuantityAvailable == null)
                yield return new ValidationResult("Количеството е задължително", new[] { nameof(QuantityAvailable) });

            if (EndsAt == null)
                yield return new ValidationResult("Крайната дата е задължителна", new[] { nameof(EndsAt) });

            if (RestaurantId == null)
                yield return new ValidationResult("Избери ресторант", new[] { nameof(RestaurantId) });

            if (CategoryId == null)
                yield return new ValidationResult("Избери категория", new[] { nameof(CategoryId) });
        }

        if (Type == "Category")
        {
            if (string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Името е задължително", new[] { nameof(Name) });
        }

        if (Type == "Tag")
        {
            if (string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Името е задължително", new[] { nameof(Name) });
        }
    }
}