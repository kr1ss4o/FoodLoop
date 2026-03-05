using System.ComponentModel.DataAnnotations;

namespace FoodLoop.Models.Entities
{
    public class Restaurant : BaseEntity
    {
        [Display(Name = "Име")]
        public string Name { get; set; }

        [Display(Name = "Бизнес имейл")]
        public string BusinessEmail { get; set; }

        [Display(Name = "Тел. номер")]
        public string Phone { get; set; }

        [Display(Name = "Адрес")]
        public string Address { get; set; }

        [Display(Name = "Адрес на изобр.")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Адрес на банер")]
        public string? BannerImageUrl { get; set; }

        [Display(Name = "Рейтинг")]
        public double Rating { get; set; }

        // FK to Owner (User)
        [Display(Name = "Собственик ID")]
        public string OwnerUserId { get; set; }
        public User Owner { get; set; }

        // Navigation
        public ICollection<Offer>? Offers { get; set; }
    }
}