using FoodLoop.Data;
using FoodLoop.Models.Enums;
using FoodLoop.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers
{
    public class RestaurantController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RestaurantController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==============================
        // GET: /Restaurant
        // ==============================
        public async Task<IActionResult> Index(string? search, string? sort)
        {
            sort = (sort ?? "").ToLower();

            // Restaurants
            var restaurants = await _context.Restaurants
                .AsNoTracking()
                .Where(r => string.IsNullOrWhiteSpace(search) || r.Name.Contains(search))
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Address,
                    r.ImageUrl,
                    r.BannerImageUrl
                })
                .ToListAsync();

            var restaurantIds = restaurants.Select(r => r.Id).ToList();

            // Active offers
            var offersByRestaurant = await _context.Offers
                .AsNoTracking()
                .Where(o => restaurantIds.Contains(o.RestaurantId) && o.QuantityAvailable > 0)
                .GroupBy(o => o.RestaurantId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            // Reviews aggregation
            var reviewsByRestaurant = await (
                from rev in _context.Reviews.AsNoTracking()
                join res in _context.Reservations on rev.ReservationId equals res.Id
                join item in _context.ReservationItems on res.Id equals item.ReservationId
                join offer in _context.Offers on item.OfferId equals offer.Id
                where res.Status == ReservationStatus.Finished
                      && restaurantIds.Contains(offer.RestaurantId)
                select new { offer.RestaurantId, rev.Id, rev.Rating }
            )
            .Distinct()
            .GroupBy(x => x.RestaurantId)
            .Select(g => new
            {
                g.Key,
                Avg = g.Average(x => (double)x.Rating),
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.Key, x => new { x.Avg, x.Count });

            // Mapping
            var result = restaurants.Select(r =>
            {
                offersByRestaurant.TryGetValue(r.Id, out var offersCount);
                reviewsByRestaurant.TryGetValue(r.Id, out var reviews);

                return new RestaurantCardViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    Address = r.Address,
                    ImageUrl = r.ImageUrl,
                    BannerImageUrl = r.BannerImageUrl,
                    ActiveOffersCount = offersCount,
                    ReviewsCount = reviews?.Count ?? 0,
                    AvgRating = reviews != null ? Math.Round(reviews.Avg, 1) : 0
                };
            }).ToList();

            // Sorting
            result = sort switch
            {
                "rating_desc" => result
                    .OrderByDescending(x => x.AvgRating)
                    .ThenByDescending(x => x.ReviewsCount)
                    .ToList(),

                "offers_desc" => result
                    .OrderByDescending(x => x.ActiveOffersCount)
                    .ToList(),

                "name_asc" => result
                    .OrderBy(x => x.Name)
                    .ToList(),

                _ => result
                    .OrderByDescending(x => x.ActiveOffersCount)
                    .ToList()
            };

            ViewBag.Search = search;
            ViewBag.Sort = sort;

            return View(result);
        }

        // ==============================
        // GET: /Restaurant/Details/{id}
        // ==============================
        public async Task<IActionResult> Details(Guid id)
        {
            // Restaurant
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Address,
                    r.ImageUrl,
                    r.BannerImageUrl
                })
                .FirstOrDefaultAsync();

            if (restaurant == null)
                return NotFound();

            // Active offers
            var activeOffers = await _context.Offers
                .AsNoTracking()
                .Where(o => o.RestaurantId == id && o.QuantityAvailable > 0)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            // Reviews
            var reviewsBase = await (
                from rev in _context.Reviews.AsNoTracking()
                join res in _context.Reservations on rev.ReservationId equals res.Id
                join u in _context.Users on res.UserId equals u.Id
                join item in _context.ReservationItems on res.Id equals item.ReservationId
                join offer in _context.Offers on item.OfferId equals offer.Id
                where res.Status == ReservationStatus.Finished
                      && offer.RestaurantId == id
                select new
                {
                    ReviewId = rev.Id,
                    rev.Rating,
                    rev.Comment,
                    AuthorName = u.FullName,
                    CreatedAt = rev.CreatedAt
                }
            )
            .Distinct()
            .ToListAsync();

            var reviewsCount = reviewsBase.Count;

            var avgRating = reviewsCount == 0
                ? 0
                : Math.Round(reviewsBase.Average(x => x.Rating), 1);

            var latestReviews = reviewsBase
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new RestaurantReviewViewModel
                {
                    Rating = x.Rating,
                    Comment = x.Comment,
                    AuthorName = string.IsNullOrWhiteSpace(x.AuthorName)
                        ? "Anonymous"
                        : x.AuthorName,
                    CreatedAt = x.CreatedAt
                })
                .ToList();

            var viewModel = new RestaurantDetailsViewModel
            {
                Id = restaurant.Id,
                Name = restaurant.Name,
                Address = restaurant.Address,
                ImageUrl = restaurant.ImageUrl,
                BannerImageUrl = restaurant.BannerImageUrl,
                AvgRating = avgRating,
                ReviewsCount = reviewsCount,
                ActiveOffers = activeOffers,
                LatestReviews = latestReviews
            };

            return View(viewModel);
        }
    }
}