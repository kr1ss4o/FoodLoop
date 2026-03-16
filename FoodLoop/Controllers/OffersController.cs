using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.Enums;
using FoodLoop.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers
{
    public class OffersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OffersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? sort)
        {
            IQueryable<Offer> query = _context.Offers
                .AsNoTracking()
                .Where(o => o.QuantityAvailable > 0 && o.EndsAt > DateTime.UtcNow)
                .Include(o => o.Restaurant)
                .Include(o => o.Category);

            // =============================
            // Sorting (без rating тук)
            // =============================

            query = sort switch
            {
                "price_asc" => query.OrderBy(o => o.DiscountedPrice),
                "price_desc" => query.OrderByDescending(o => o.DiscountedPrice),

                "newest" => query.OrderByDescending(o => o.CreatedAt),
                "oldest" => query.OrderBy(o => o.CreatedAt),

                _ => query.OrderByDescending(o => o.CreatedAt)
            };

            // =============================
            // Restaurant Ratings
            // =============================

            var restaurantRatings = await _context.Reviews
                .Include(r => r.Reservation)
                    .ThenInclude(res => res.Items)
                        .ThenInclude(i => i.Offer)
                .SelectMany(r => r.Reservation.Items.Select(i => new
                {
                    RestaurantId = i.Offer.RestaurantId,
                    Rating = r.Rating
                }))
                .GroupBy(x => x.RestaurantId)
                .Select(g => new
                {
                    RestaurantId = g.Key,
                    Rating = g.Average(x => x.Rating)
                })
                .ToDictionaryAsync(x => x.RestaurantId, x => x.Rating);

            // =============================
            // Get Offers
            // =============================

            var offers = await query.ToListAsync();

            // =============================
            // Rating Sort (FIX)
            // =============================

            if (sort == "rating_desc")
            {
                offers = offers
                    .OrderByDescending(o =>
                        restaurantRatings.ContainsKey(o.RestaurantId)
                            ? restaurantRatings[o.RestaurantId]
                            : o.Restaurant?.Rating ?? 0)
                    .ThenByDescending(o => o.CreatedAt) // стабилен sort
                    .ToList();
            }

            // =============================
            // Trending Restaurants (Top 3)
            // =============================

            var trendingRestaurants = await _context.Reservations
                .Where(r => r.Status == ReservationStatus.Finished)
                .SelectMany(r => r.Items)
                .GroupBy(i => i.Offer.Restaurant)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToListAsync();

            // =============================
            // ViewModel
            // =============================

            var vm = new MarketplaceViewModel
            {
                Offers = offers,
                Sort = sort
            };

            ViewBag.TrendingRestaurants = trendingRestaurants;
            ViewBag.RestaurantRatings = restaurantRatings;

            return View(vm);
        }

        // =============================
        // Offer Details
        // =============================

        public async Task<IActionResult> Details(Guid id)
        {
            var offer = await _context.Offers
                .Include(o => o.Restaurant)
                .Include(o => o.OfferTags)
                    .ThenInclude(ot => ot.Tag)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offer == null)
                return NotFound();

            return View(offer);
        }
    }
}