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
            int page = 1)
        {
            const int pageSize = 6;

            IQueryable<Offer> offersQuery = _context.Offers
            .AsNoTracking()
            .Include(o => o.Restaurant)
            .Include(o => o.Category);

            IQueryable<Restaurant> restaurantsQuery = _context.Restaurants
            .AsNoTracking()
            .Include(r => r.Offers);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var normalized = SearchHelper.Normalize(query);

                if (!string.IsNullOrEmpty(normalized))
                {
                    offersQuery = offersQuery.Where(o =>
                        EF.Functions.Like(o.Title.ToLower(), $"%{normalized}%") ||
                        EF.Functions.Like(o.Restaurant.Name.ToLower(), $"%{normalized}%") ||
                        EF.Functions.Like(o.Category.Name.ToLower(), $"%{normalized}%"));

                    restaurantsQuery = restaurantsQuery
                        .Where(r => EF.Functions.Like(r.Name.ToLower(), $"%{normalized}%"));
                }


                offersQuery = offersQuery.Where(o =>
                    o.Title.ToLower().Contains(normalized) ||
                    o.Restaurant.Name.ToLower().Contains(normalized) ||
                    o.Category.Name.ToLower().Contains(normalized));

                restaurantsQuery = restaurantsQuery
                    .Where(r => r.Name.ToLower().Contains(normalized));

            }

            if (categoryId.HasValue)
            {
                offersQuery = offersQuery
                    .Where(o => o.CategoryId == categoryId);
            }

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

            var (offers, totalPages) =
                await offersQuery.ToPagedListAsync(page, pageSize);

            var restaurants = await restaurantsQuery
                .Take(6)
                .ToListAsync();

            var vm = new SearchViewModel
            {
                Query = query,
                Offers = offers,
                Restaurants = restaurants,
                CurrentPage = page,
                TotalPages = totalPages,
                Sort = sort,
                CategoryId = categoryId
            };

            return View(vm);
        }
    }
}