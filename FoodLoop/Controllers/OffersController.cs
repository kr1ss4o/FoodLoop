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

        public async Task<IActionResult> Index(string? search, string? sort)
        {
            var query = _context.Offers
                .Where(o => o.QuantityAvailable > 0)
                .Include(o => o.Restaurant)
                .Include(o => o.OfferTags)
                    .ThenInclude(ot => ot.Tag)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(o =>
                    o.Title.Contains(search) ||
                    o.Restaurant.Name.Contains(search) ||
                    o.OfferTags.Any(ot => ot.Tag.Name.Contains(search)));
            }

            query = (sort ?? "").ToLower() switch
            {
                "price_asc" => query.OrderBy(o => o.DiscountedPrice),
                "price_desc" => query.OrderByDescending(o => o.DiscountedPrice),
                "newest" => query.OrderByDescending(o => o.CreatedAt),
                _ => query.OrderByDescending(o => o.CreatedAt)
            };

            var model = await query.ToListAsync();
            return View(model);
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