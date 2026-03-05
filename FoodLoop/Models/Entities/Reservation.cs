using FoodLoop.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace FoodLoop.Models.Entities
{
    public class Reservation : BaseEntity
    {
        [Display(Name = "Клиент ID")]
        public string UserId { get; set; } = null!;

        [Display(Name = "Клиент")]
        public User User { get; set; } = null!;

        [Display(Name = "Статус")]
        public ReservationStatus Status { get; set; }

        [Display(Name = "Създадена на")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Тип доставка")]
        public string DeliveryType { get; set; } = "Pickup";

        [Display(Name = "Адрес")]
        public string? DeliveryAddress { get; set; }

        [Display(Name = "Обща сума")]
        public decimal TotalPrice { get; set; }

        [Display(Name = "Поръчка за друг")]
        public bool IsForSomeoneElse { get; set; }

        [Display(Name = "Получател")]
        public string? RecipientFullName { get; set; }

        [Display(Name = "Телефон на получател")]
        public string? RecipientPhone { get; set; }


        public Review? Review { get; set; }
        public ICollection<ReservationStatusLog> StatusLogs { get; set; } = new List<ReservationStatusLog>();
        public ICollection<ReservationItem> Items { get; set; } = new List<ReservationItem>();
    }

}