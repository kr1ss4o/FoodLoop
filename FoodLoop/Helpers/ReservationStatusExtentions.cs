using FoodLoop.Models.Enums;

namespace FoodLoop.Helpers
{
    public static class ReservationStatusExtensions
    {
        public static string ToBg(this ReservationStatus status)
        {
            return status switch
            {
                ReservationStatus.Pending => "Очаква потвърждение",
                ReservationStatus.Confirmed => "Потвърдена",
                ReservationStatus.OutForDelivery => "На път",
                ReservationStatus.Finished => "Завършена",
                ReservationStatus.Canceled => "Отказана",
                _ => status.ToString()
            };
        }
    }
}