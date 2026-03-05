using System.ComponentModel.DataAnnotations;

namespace FoodLoop.Models.Entities
{
    public class Category : BaseEntity
    {
        [Display (Name="Име")]
        public string Name { get; set; }

        [Display(Name = "Адрес на изобр.")]
        public string? IconUrl { get; set; }

        public ICollection<Offer>? Offers { get; set; }
    }
}