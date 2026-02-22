using FoodLoop.Models.ViewModels;

namespace FoodLoop.Services.Interfaces
{
    public interface IDashboardAnalyticsService
    {
        Task<RestaurantDashboardViewModel> BuildDashboardAsync(string userId, DateTime? date);
    }
}