namespace FoodLoop.Services.Interfaces
{
    public interface IReservationService
    {
        Task ExpirePendingReservationsAsync();
    }
}
