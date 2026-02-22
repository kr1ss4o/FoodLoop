using FoodLoop.Data;
using FoodLoop.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers
{
    [Authorize]
    public class OffersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OffersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string? sort, Guid? categoryId, int page = 1)
        {
            const int pageSize = 3;

            var query = _context.Offers
                .Where(o => o.QuantityAvailable > 0)
                .Include(o => o.Restaurant)
                .Include(o => o.Category)
                .Include(o => o.OfferTags)
                    .ThenInclude(ot => ot.Tag)
                .AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(o =>
                    o.Title.Contains(search) ||
                    o.Restaurant.Name.Contains(search) ||
                    o.OfferTags.Any(t => t.Tag.Name.Contains(search)));
            }

            // CATEGORY FILTER
            if (categoryId.HasValue)
            {
                query = query.Where(o => o.CategoryId == categoryId);
            }

            // SORT
            query = (sort ?? "").ToLower() switch
            {
                "price_asc" => query.OrderBy(o => o.DiscountedPrice),
                "price_desc" => query.OrderByDescending(o => o.DiscountedPrice),
                "rating_desc" => query.OrderByDescending(o => o.Restaurant.Rating),
                _ => query.OrderByDescending(o => o.CreatedAt)
            };

            var totalItems = await query.CountAsync();

            var offers = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Search = search;
            ViewBag.Sort = sort;
            ViewBag.CategoryId = categoryId;
            ViewBag.Categories = await _context.Categories.AsNoTracking().ToListAsync();

            return View(offers);
        }

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