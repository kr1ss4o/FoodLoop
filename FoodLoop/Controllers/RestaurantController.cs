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

            // 1) Restaurants (само нужните колони)
            var restaurants = await _context.Restaurants
                .Where(r => string.IsNullOrWhiteSpace(search) || r.Name.Contains(search))
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Address,
                    r.ImageUrl
                })
                .ToListAsync();

            var restaurantIds = restaurants.Select(r => r.Id).ToList();

            // 2) Active offers count per restaurant
            var offersCounts = await _context.Offers
                .Where(o => restaurantIds.Contains(o.RestaurantId) && o.QuantityAvailable > 0)
                .GroupBy(o => o.RestaurantId)
                .Select(g => new { RestaurantId = g.Key, Count = g.Count() })
                .ToListAsync();

            var offersByRestaurant = offersCounts.ToDictionary(x => x.RestaurantId, x => x.Count);

            // 3) Reviews agg per restaurant (Avg + Count) for finished reservations
            var reviewsAgg = await (
                from rev in _context.Reviews
                join res in _context.Reservations on rev.ReservationId equals res.Id
                join item in _context.ReservationItems on res.Id equals item.ReservationId
                join offer in _context.Offers on item.OfferId equals offer.Id
                where res.Status == ReservationStatus.Finished
                      && restaurantIds.Contains(offer.RestaurantId)
                select new { offer.RestaurantId, rev.Id, rev.Rating }
            )
            .Distinct() // гарантира, че ако има multiple items към същия ресторант, ревюто се брои 1 път
            .GroupBy(x => x.RestaurantId)
            .Select(g => new
            {
                RestaurantId = g.Key,
                Avg = g.Average(x => (double)x.Rating),
                Count = g.Count()
            })
            .ToListAsync();

            var reviewsByRestaurant = reviewsAgg.ToDictionary(
                x => x.RestaurantId,
                x => new { x.Avg, x.Count }
            );

            // Map -> ViewModel
            var result = restaurants.Select(r =>
            {
                var offersCount = offersByRestaurant.TryGetValue(r.Id, out var oc) ? oc : 0;
                var hasReviews = reviewsByRestaurant.TryGetValue(r.Id, out var ra);

                return new RestaurantCardViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    Address = r.Address,
                    ImageUrl = r.ImageUrl,
                    ActiveOffersCount = offersCount,
                    ReviewsCount = hasReviews ? ra!.Count : 0,
                    AvgRating = hasReviews ? Math.Round(ra!.Avg, 1) : 0
                };
            }).ToList();

            // Sorting (in-memory, защото вече имаме всичко)
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
            // 1) Restaurant (само нужните колони)
            var restaurant = await _context.Restaurants
                .Where(r => r.Id == id)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Address,
                    r.ImageUrl
                })
                .FirstOrDefaultAsync();

            if (restaurant == null)
                return NotFound();

            // 2) Active offers
            var activeOffers = await _context.Offers
                .Where(o => o.RestaurantId == id && o.QuantityAvailable > 0)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            // 3) Reviews base (1 път) -> distinct по ReviewId (за да няма duplicates от multiple items)
            var reviewsBase = await (
                from rev in _context.Reviews
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

            // Aggregate in-memory (чисто + без EF преводи)
            var reviewsCount = reviewsBase.Count;
            var avgRating = reviewsCount == 0 ? 0 : Math.Round(reviewsBase.Average(x => x.Rating), 1);

            var latestReviews = reviewsBase
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .Select(x => new RestaurantReviewViewModel
                {
                    Rating = x.Rating,
                    Comment = x.Comment,
                    AuthorName = string.IsNullOrWhiteSpace(x.AuthorName) ? "Anonymous" : x.AuthorName,
                    CreatedAt = x.CreatedAt
                })
                .ToList();

            var viewModel = new RestaurantDetailsViewModel
            {
                Id = restaurant.Id,
                Name = restaurant.Name,
                Address = restaurant.Address,
                ImageUrl = restaurant.ImageUrl,
                AvgRating = avgRating,
                ReviewsCount = reviewsCount,
                ActiveOffers = activeOffers,
                LatestReviews = latestReviews
            };

            return View(viewModel);
        }
    }
}