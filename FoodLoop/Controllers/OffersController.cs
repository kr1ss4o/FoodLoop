using FoodLoop.Data;
using FoodLoop.Helpers;
using FoodLoop.Models.Entities;
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
                .Where(o => o.QuantityAvailable > 0)
                .Include(o => o.Restaurant)
                .Include(o => o.Category);

            // Закуска
            var breakfastQuery = baseQuery
                .Where(o => o.Category.Name == "Закуска")
                .OrderByDescending(o => o.CreatedAt);

            var (breakfastOffers, breakfastTotalPages) =
                await breakfastQuery.ToPagedListAsync(breakfastPage, pageSize);

            // Обяд
            var lunchQuery = baseQuery
                .Where(o => o.Category.Name == "Обяд")
                .OrderByDescending(o => o.CreatedAt);

            var (lunchOffers, lunchTotalPages) =
                await lunchQuery.ToPagedListAsync(lunchPage, pageSize);

            // Вечеря
            var dinnerQuery = baseQuery
                .Where(o => o.Category.Name == "Вечеря")
                .OrderByDescending(o => o.CreatedAt);

            var (dinnerOffers, dinnerTotalPages) =
                await dinnerQuery.ToPagedListAsync(dinnerPage, pageSize);

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

            return View(vm);
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