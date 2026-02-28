using System.ComponentModel.DataAnnotations;

namespace FoodLoop.Models.Enums
{
    public enum ReservationStatus
    {
        [Display(Name = "Изчакваща")]
        Pending = 0,

        [Display(Name = "Потвърдена")]
        Confirmed = 1,

        [Display(Name = "На път")]
        OutForDelivery = 2,

        [Display(Name = "Завършена")]
        Finished = 3,

        [Display(Name = "Отказана")]
        Canceled = 4
    }
}