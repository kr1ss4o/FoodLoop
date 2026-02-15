using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers
{
    [Authorize(Roles = "Restaurant")]
    public class OfferManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public OfferManagerController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============================================================
        // INDEX
        // ============================================================
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

            if (restaurant == null)
                return Unauthorized();

            var offers = await _context.Offers
                .Where(o => o.RestaurantId == restaurant.Id)
                .AsNoTracking()
                .ToListAsync();

            return View("/Views/Restaurant/OfferManager/Index.cshtml", offers);
        }

        // ============================================================
        // CREATE - GET
        // ============================================================
        public async Task<IActionResult> Create()
        {
            return View("/Views/Restaurant/OfferManager/Create.cshtml",
                await BuildFormViewModel(new Offer()));
        }

        // ============================================================
        // CREATE - POST
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OfferFormViewModel model, IFormFile? ImageUpload)
        {
            // 🔥 Remove navigation validation issues
            ModelState.Remove("Offer.Restaurant");
            ModelState.Remove("Offer.Category");
            ModelState.Remove("Offer.OfferTags");

            if (!ModelState.IsValid)
                return View("/Views/Restaurant/OfferManager/Create.cshtml",
                    await RebuildViewModel(model));

            var user = await _userManager.GetUserAsync(User);
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == user!.Id);

            if (restaurant == null)
                return Unauthorized();

            if (ImageUpload != null)
                model.Offer.ImageUrl = await SaveImage(ImageUpload);

            model.Offer.RestaurantId = restaurant.Id;

            _context.Offers.Add(model.Offer);
            await _context.SaveChangesAsync();

            await UpdateTags(model.Offer.Id, model.SelectedTags);

            TempData["Success"] = "Offer created successfully!";
            return RedirectToAction("Index");
        }

        // ============================================================
        // EDIT - GET
        // ============================================================
        public async Task<IActionResult> Edit(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);

            var offer = await _context.Offers
                .Include(o => o.OfferTags)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offer == null)
                return NotFound();

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == user!.Id);

            if (restaurant == null || offer.RestaurantId != restaurant.Id)
                return Unauthorized();

            return View("/Views/Restaurant/OfferManager/Edit.cshtml",
                await BuildFormViewModel(offer));
        }

        // ============================================================
        // EDIT - POST
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OfferFormViewModel model, IFormFile? ImageUpload)
        {
            ModelState.Remove("Offer.Restaurant");
            ModelState.Remove("Offer.Category");
            ModelState.Remove("Offer.OfferTags");

            if (!ModelState.IsValid)
                return View("/Views/Restaurant/OfferManager/Edit.cshtml",
                    await RebuildViewModel(model));

            var user = await _userManager.GetUserAsync(User);

            var offer = await _context.Offers
                .Include(o => o.OfferTags)
                .FirstOrDefaultAsync(o => o.Id == model.Offer.Id);

            if (offer == null)
                return NotFound();

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == user!.Id);

            if (restaurant == null || offer.RestaurantId != restaurant.Id)
                return Unauthorized();

            // Update fields
            offer.Title = model.Offer.Title;
            offer.Description = model.Offer.Description;
            offer.OriginalPrice = model.Offer.OriginalPrice;
            offer.DiscountedPrice = model.Offer.DiscountedPrice;
            offer.QuantityAvailable = model.Offer.QuantityAvailable;
            offer.CategoryId = model.Offer.CategoryId;
            offer.EndsAt = model.Offer.EndsAt;

            if (ImageUpload != null)
                offer.ImageUrl = await SaveImage(ImageUpload);

            await UpdateTags(offer.Id, model.SelectedTags);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Offer updated successfully!";
            return RedirectToAction("Index");
        }

        // ============================================================
        // DELETE
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);

            var offer = await _context.Offers
                .Include(o => o.OfferTags)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offer == null)
                return NotFound();

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == user!.Id);

            if (restaurant == null || offer.RestaurantId != restaurant.Id)
                return Unauthorized();

            _context.OfferTags.RemoveRange(offer.OfferTags);
            _context.Offers.Remove(offer);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Offer deleted successfully!";
            return RedirectToAction("Index");
        }

        // ============================================================
        // 🔧 HELPERS
        // ============================================================

        private async Task<string> SaveImage(IFormFile file)
        {
            var dir = Path.Combine("wwwroot", "images", "offers");
            Directory.CreateDirectory(dir);

            var filename = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(dir, filename);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/images/offers/{filename}";
        }

        private async Task UpdateTags(Guid offerId, List<Guid>? selectedTags)
        {
            var existing = await _context.OfferTags
                .Where(t => t.OfferId == offerId)
                .ToListAsync();

            _context.OfferTags.RemoveRange(existing);

            if (selectedTags != null)
            {
                foreach (var tagId in selectedTags)
                {
                    _context.OfferTags.Add(new OfferTag
                    {
                        OfferId = offerId,
                        TagId = tagId
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task<OfferFormViewModel> BuildFormViewModel(Offer offer)
        {
            return new OfferFormViewModel
            {
                Offer = offer,
                Categories = await _context.Categories.AsNoTracking().ToListAsync(),
                Tags = await _context.Tags.AsNoTracking().ToListAsync(),
                SelectedTags = offer.OfferTags?.Select(t => t.TagId).ToList() ?? new List<Guid>()
            };
        }

        private async Task<OfferFormViewModel> RebuildViewModel(OfferFormViewModel model)
        {
            model.Categories = await _context.Categories.AsNoTracking().ToListAsync();
            model.Tags = await _context.Tags.AsNoTracking().ToListAsync();
            return model;
        }
    }
}