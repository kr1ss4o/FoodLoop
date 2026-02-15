using FoodLoop.Data;
using FoodLoop.Models.Entities;
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

        public async Task<IActionResult> Index(string? search, string? sort)
        {
            var query = _context.Offers
                .Include(o => o.Restaurant)
                .Include(o => o.OfferTags)
                    .ThenInclude(ot => ot.Tag)
                .Where(o => o.QuantityAvailable > 0)
                .AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();

                query = query.Where(o =>
                    o.Title.ToLower().Contains(search) ||
                    o.Restaurant.Name.ToLower().Contains(search) ||
                    o.OfferTags.Any(t => t.Tag.Name.ToLower().Contains(search))
                );
            }

            // SORT
            query = sort switch
            {
                "price_asc" => query.OrderBy(o => o.DiscountedPrice),
                "price_desc" => query.OrderByDescending(o => o.DiscountedPrice),
                "newest" => query.OrderByDescending(o => o.CreatedAt),
                _ => query.OrderByDescending(o => o.CreatedAt)
            };

            var offers = await _context.Offers
            .Include(o => o.Restaurant)
            .Where(o =>
            o.QuantityAvailable > 0 &&
            o.EndsAt > DateTime.UtcNow)
            .ToListAsync();

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