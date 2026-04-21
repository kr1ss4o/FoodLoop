using FoodLoop.Data;
using FoodLoop.Helpers;
using FoodLoop.Models.Entities;
using FoodLoop.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string query,
            string? sort,
            Guid? categoryId,
            int offersPage = 1,
            int restaurantsPage = 1)
        {
            const int pageSize = 8;

            IQueryable<Offer> offersQuery = _context.Offers
                .AsNoTracking()
                .Include(o => o.Restaurant)
                .Include(o => o.Category)
                .Include(o => o.OfferTags)
                    .ThenInclude(ot => ot.Tag);

            IQueryable<Restaurant> restaurantsQuery = _context.Restaurants
                .AsNoTracking()
                .Include(r => r.Offers);

            // =============================
            // SEARCH
            // =============================

            if (!string.IsNullOrWhiteSpace(query))
            {
                var normalized = SearchHelper.Normalize(query);

                if (!string.IsNullOrEmpty(normalized))
                {
                    offersQuery = offersQuery
                        .Include(o => o.OfferTags)
                            .ThenInclude(ot => ot.Tag)
                        .Where(o =>
                            o.Title.ToLower().Contains(normalized) ||
                            o.Restaurant.Name.ToLower().Contains(normalized) ||
                            o.Category.Name.ToLower().Contains(normalized) ||
                            o.OfferTags.Any(ot => ot.Tag.Name.ToLower().Contains(normalized))
                        );

                    restaurantsQuery = restaurantsQuery
                        .Where(r => r.Name.ToLower().Contains(normalized));
                }
            }

            // =============================
            // CATEGORY FILTER
            // =============================

            if (categoryId.HasValue)
            {
                offersQuery = offersQuery
                    .Where(o => o.CategoryId == categoryId);
            }

            // =============================
            // SORTING
            // =============================

            offersQuery = (sort ?? "").ToLower() switch
            {
                "price_asc" => offersQuery
                    .OrderBy(o => o.DiscountedPrice),

                "price_desc" => offersQuery
                    .OrderByDescending(o => o.DiscountedPrice),

                "rating_desc" => offersQuery
                    .OrderByDescending(o => o.Restaurant.Rating)
                    .ThenByDescending(o => o.CreatedAt),

                "expiring_soon" => offersQuery
                    .OrderBy(o => o.EndsAt)
                    .ThenByDescending(o => o.Restaurant.Rating),

                _ => offersQuery
                    .OrderByDescending(o => o.CreatedAt)
            };

            // =============================
            // COUNTS
            // =============================

            var offersCount = await offersQuery.CountAsync();
            var restaurantsCount = await restaurantsQuery.CountAsync();

            // =============================
            // PAGINATION (разделена)
            // =============================

            var (offers, totalOfferPages) =
                await offersQuery.ToPagedListAsync(offersPage, pageSize);

            var (restaurants, totalRestaurantPages) =
                await restaurantsQuery.ToPagedListAsync(restaurantsPage, pageSize);

            // =============================
            // CALCULATE RESTAURANT RATINGS
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
            // APPLY RATINGS
            // =============================

            foreach (var offer in offers)
            {
                if (restaurantRatings.TryGetValue(offer.RestaurantId, out var rating))
                {
                    offer.Restaurant.Rating = rating;
                }
            }

            foreach (var restaurant in restaurants)
            {
                if (restaurantRatings.TryGetValue(restaurant.Id, out var rating))
                {
                    restaurant.Rating = rating;
                }
            }

            // =============================
            // VIEWMODEL
            // =============================

            var vm = new SearchViewModel
            {
                Query = query,
                Offers = offers,
                Restaurants = restaurants,
                CurrentPage = offersPage,
                TotalPages = totalOfferPages,
                RestaurantsCurrentPage = restaurantsPage,
                RestaurantPages = totalRestaurantPages,
                Sort = sort,
                CategoryId = categoryId,
                OffersCount = offersCount,
                RestaurantsCount = restaurantsCount
            };

            return View(vm);
        }
    }
}