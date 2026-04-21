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

        public async Task<IActionResult> Index(string? sort, int page = 1)
        {
            const int pageSize = 3;

            // =============================
            // Base Query
            // =============================

            IQueryable<Offer> query = _context.Offers
                .AsNoTracking()
                .Where(o => o.QuantityAvailable > 0 && o.EndsAt > DateTime.UtcNow)
                .Include(o => o.Restaurant)
                .Include(o => o.Category);

            //
            // Categories
            //

            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = categories;

            // =============================
            // Restaurant Ratings (avg + count)
            // =============================

            var restaurantRatingsRaw = await _context.Reviews
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
                    AvgRating = g.Average(x => x.Rating),
                    Count = g.Count()
                })
                .ToListAsync();

            // =============================
            // Smart Score (rating + volume)
            // =============================

            var restaurantScores = restaurantRatingsRaw
                .ToDictionary(
                    x => x.RestaurantId,
                    x => x.AvgRating * Math.Log(1 + x.Count)
                );

            var restaurantRatings = restaurantRatingsRaw
                .ToDictionary(
                    x => x.RestaurantId,
                    x => x.AvgRating
                );

            // =============================
            // Get Offers
            // =============================

            var offers = await query.ToListAsync();

            // =============================
            // Sorting
            // =============================

            offers = sort switch
            {
                "price_asc" => offers.OrderBy(o => o.DiscountedPrice).ToList(),

                "price_desc" => offers.OrderByDescending(o => o.DiscountedPrice).ToList(),

                "oldest" => offers.OrderBy(o => o.CreatedAt).ToList(),

                "rating_desc" => offers
                    .OrderByDescending(o =>
                        restaurantScores.ContainsKey(o.RestaurantId)
                            ? restaurantScores[o.RestaurantId]
                            : 0)
                    .ThenByDescending(o => o.CreatedAt)
                    .ToList(),

                _ => offers.OrderByDescending(o => o.CreatedAt).ToList()
            };

            // =============================
            // Pagination
            // =============================

            var totalItems = offers.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var pagedOffers = offers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // =============================
            // Trending Restaurants (SMART)
            // =============================

            var trendingRestaurants = await _context.Reviews
                .Include(r => r.Reservation)
                    .ThenInclude(res => res.Items)
                        .ThenInclude(i => i.Offer)
                            .ThenInclude(o => o.Restaurant)
                .SelectMany(r => r.Reservation.Items.Select(i => new
                {
                    Restaurant = i.Offer.Restaurant,
                    Rating = r.Rating
                }))
                .GroupBy(x => x.Restaurant)
                .Select(g => new
                {
                    Restaurant = g.Key,
                    AvgRating = g.Average(x => x.Rating),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.AvgRating * Math.Log(1 + x.Count))
                .Take(3)
                .Select(x => x.Restaurant)
                .ToListAsync();

            // =============================
            // ViewModel
            // =============================

            var vm = new MarketplaceViewModel
            {
                Offers = pagedOffers,
                Sort = sort,
                CurrentPage = page,
                TotalPages = totalPages
            };

            ViewBag.TrendingRestaurants = trendingRestaurants;
            ViewBag.RestaurantRatings = restaurantRatings;

            return View(vm);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var offer = _context.Offers
            .Include(o => o.Restaurant)
            .Include(o => o.Category)
            .Include(o => o.OfferTags)
            .ThenInclude(ot => ot.Tag)
            .FirstOrDefault(o => o.Id == id);

            if (offer == null)
                return NotFound();

            // SAFE rating
            var ratings = await _context.Reviews
            .Include(r => r.Reservation)
            .ThenInclude(res => res.Items)
            .Where(r => r.Reservation != null &&
                r.Reservation.Items.Any(i => i.Offer.RestaurantId == offer.RestaurantId))
            .Select(r => r.Rating)
            .ToListAsync();

            ViewBag.AvgRating = ratings.Count > 0 ? ratings.Average() : 0;
            ViewBag.ReviewsCount = ratings.Count;

            // All reviews for the restaurant info page in details
            var latestReviews = _context.Reviews
            .Where(r => r.Reservation.Items
                .Any(i => i.Offer.RestaurantId == offer.RestaurantId))
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new {
                r.Rating,
                r.Comment,
                r.CreatedAt,
                AuthorName = !string.IsNullOrEmpty(r.Reservation.User.FullName)
                    ? r.Reservation.User.FullName
                    : r.Reservation.User.UserName
            }).ToList();

            ViewBag.LatestReviews = latestReviews;

            // Other offers from the same restaurant
            var relatedOffers = await _context.Offers
                .AsNoTracking()
                .Where(o => o.RestaurantId == offer.RestaurantId
                         && o.Id != offer.Id
                         && o.QuantityAvailable > 0
                         && o.EndsAt > DateTime.UtcNow)
                .Include(o => o.Restaurant)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.RelatedOffers = relatedOffers;

            return View(offer);
        }

        public async Task<IActionResult> PagedOffers(string? sort, int page = 1)
        {
            const int pageSize = 3;

            IQueryable<Offer> query = _context.Offers
                .AsNoTracking()
                .Where(o => o.QuantityAvailable > 0 && o.EndsAt > DateTime.UtcNow)
                .Include(o => o.Restaurant)
                .Include(o => o.Category);

            var offers = await query.ToListAsync();

            offers = sort switch
            {
                "price_asc" => offers.OrderBy(o => o.DiscountedPrice).ToList(),
                "price_desc" => offers.OrderByDescending(o => o.DiscountedPrice).ToList(),
                "oldest" => offers.OrderBy(o => o.CreatedAt).ToList(),
                _ => offers.OrderByDescending(o => o.CreatedAt).ToList()
            };

            var pagedOffers = offers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return PartialView("~/Views/Shared/_OffersGrid.cshtml", pagedOffers);
        }
    }
}