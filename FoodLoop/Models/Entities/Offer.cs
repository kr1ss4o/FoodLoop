using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodLoop.Models.Entities
{
    public class Offer : BaseEntity
    {
        [Display(Name = "Заглавие")]
        public string Title { get; set; } = "";

        [Display(Name = "Описание")]
        public string Description { get; set; } = "";

        [Display(Name = "Редовна цена")]
        public decimal OriginalPrice { get; set; }

        [Display(Name = "Намалена цена")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountedPrice { get; set; }

        [Display(Name = "Количество")]
        public int QuantityAvailable { get; set; }


        [Display(Name = "Приключва")]
        public DateTime EndsAt { get; set; }

        [Display(Name = "Адрес на изобр.")]
        public string? ImageUrl { get; set; }

        // FK to Restaurant
        public Guid RestaurantId { get; set; }

        [Display(Name = "Ресторант")]
        public Restaurant? Restaurant { get; set; }

        // FK to Category
        public Guid CategoryId { get; set; }

        [Display(Name = "Категория")]
        public Category? Category { get; set; }

        // Many-to-Many
        public ICollection<OfferTag>? OfferTags { get; set; } = new List<OfferTag>();

        public ICollection<Reservation>? Reservations { get; set; }
        public bool IsLowStock => QuantityAvailable <= 3;
    }
}