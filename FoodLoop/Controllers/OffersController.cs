using FoodLoop.Data;
using FoodLoop.Helpers;
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

        public async Task<IActionResult> Index(
            int breakfastPage = 1,
            int lunchPage = 1,
            int dinnerPage = 1)
        {
            const int pageSize = 4;

            IQueryable<Offer> baseQuery = _context.Offers
                .AsNoTracking()
                .Where(o => o.QuantityAvailable > 0 && o.EndsAt > DateTime.UtcNow)
                .Include(o => o.Restaurant)
                .Include(o => o.Category);

            // =============================
            // Закуска
            // =============================

            var breakfastQuery = baseQuery
                .Where(o => o.Category.Name == "Закуска")
                .OrderByDescending(o => o.EndsAt);

            var (breakfastOffers, breakfastTotalPages) =
                await breakfastQuery.ToPagedListAsync(breakfastPage, pageSize);

            // =============================
            // Обяд
            // =============================

            var lunchQuery = baseQuery
                .Where(o => o.Category.Name == "Обяд")
                .OrderByDescending(o => o.EndsAt);

            var (lunchOffers, lunchTotalPages) =
                await lunchQuery.ToPagedListAsync(lunchPage, pageSize);

            // =============================
            // Вечеря
            // =============================

            var dinnerQuery = baseQuery
                .Where(o => o.Category.Name == "Вечеря")
                .OrderByDescending(o => o.EndsAt);

            var (dinnerOffers, dinnerTotalPages) =
                await dinnerQuery.ToPagedListAsync(dinnerPage, pageSize);

            // =============================
            // Trending Restaurants
            // =============================

            var lastWeek = DateTime.UtcNow.AddDays(-7);

            var trendingRestaurants = await _context.Reservations
                .Where(r => r.Status == ReservationStatus.Finished &&
                            r.CreatedAt >= lastWeek)
                .SelectMany(r => r.Items)
                .GroupBy(i => i.Offer.Restaurant)
                .OrderByDescending(g => g.Count())
                .Take(6)
                .Select(g => g.Key)
                .ToListAsync();

            // =============================
            // Trending Offers
            // =============================

            var trendingOfferIds = await _context.Reservations
                .Where(r => r.Status == ReservationStatus.Finished &&
                            r.CreatedAt >= lastWeek)
                .SelectMany(r => r.Items)
                .GroupBy(i => i.OfferId)
                .OrderByDescending(g => g.Count())
                .Take(6)
                .Select(g => g.Key)
                .ToListAsync();

            var trendingOffers = await _context.Offers
                .Where(o => trendingOfferIds.Contains(o.Id))
                .Include(o => o.Restaurant)
                .ToListAsync();

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
            // Offer Ratings
            // =============================

            var offerRatings = await _context.Reviews
                .Include(r => r.Reservation)
                    .ThenInclude(res => res.Items)
                .SelectMany(r => r.Reservation.Items.Select(i => new
                {
                    OfferId = i.OfferId,
                    Rating = r.Rating
                }))
                .GroupBy(x => x.OfferId)
                .Select(g => new
                {
                    OfferId = g.Key,
                    Rating = g.Average(x => x.Rating)
                })
                .ToDictionaryAsync(x => x.OfferId, x => x.Rating);

            // =============================
            // ViewModel
            // =============================

            var vm = new MarketplaceViewModel
            {
                BreakfastOffers = breakfastOffers,
                LunchOffers = lunchOffers,
                DinnerOffers = dinnerOffers,

                BreakfastPage = breakfastPage,
                LunchPage = lunchPage,
                DinnerPage = dinnerPage,

                BreakfastTotalPages = breakfastTotalPages,
                LunchTotalPages = lunchTotalPages,
                DinnerTotalPages = dinnerTotalPages
            };

            ViewBag.TrendingRestaurants = trendingRestaurants;
            ViewBag.TrendingOffers = trendingOffers;
            ViewBag.RestaurantRatings = restaurantRatings;
            ViewBag.OfferRatings = offerRatings;

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