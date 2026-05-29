using FoodLoop.Data;
using FoodLoop.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Helpers;

public class RatingLoader
{
    private readonly ApplicationDbContext _context;

    public RatingLoader(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RestaurantRatingStats>> LoadRestaurantRatingStatsAsync()
    {
        var ratingStats = await _context.Reviews
            .AsNoTracking()
            .SelectMany(r => r.Reservation.Items.Select(i => new
            {
                RestaurantId = i.Offer.RestaurantId,
                Rating = r.Rating
            }))
            .GroupBy(x => x.RestaurantId)
            .Select(g => new RestaurantRatingStats
            {
                RestaurantId = g.Key,
                AvgRating = g.Average(x => x.Rating),
                Count = g.Count()
            })
            .ToListAsync();

        var restaurantIds = ratingStats
            .Select(x => x.RestaurantId)
            .ToList();

        var restaurants = await _context.Restaurants
            .AsNoTracking()
            .Where(r => restaurantIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id);

        foreach (var ratingStat in ratingStats)
        {
            restaurants.TryGetValue(ratingStat.RestaurantId, out var restaurant);
            ratingStat.Restaurant = restaurant;
        }

        return ratingStats;
    }

    public async Task<double> LoadAverageRatingAsync(Guid restaurantId)
    {
        var ratings = await RestaurantReviewsQuery(restaurantId)
            .Select(r => r.Rating)
            .ToListAsync();

        return RatingHelper.Average(ratings);
    }

    public async Task<int> LoadReviewsCountAsync(Guid restaurantId)
    {
        return await RestaurantReviewsQuery(restaurantId)
            .CountAsync();
    }

    private IQueryable<Review> RestaurantReviewsQuery(Guid restaurantId)
    {
        return _context.Reviews
            .AsNoTracking()
            .Where(r => r.Reservation.Items
                .Any(i => i.Offer.RestaurantId == restaurantId));
    }
}

public class RestaurantRatingStats
{
    public Guid RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }
    public double AvgRating { get; set; }
    public int Count { get; set; }
}
