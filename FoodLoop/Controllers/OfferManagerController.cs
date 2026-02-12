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
        // INDEX — SHOW GRID OF OFFERS
        // ROUTE: /OfferManager
        // ============================================================
        [HttpGet("/OfferManager")]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

            if (restaurant == null)
            {
                TempData["Error"] = "Restaurant not found.";
                return RedirectToAction("Index", "Home");
            }

            var offers = await _context.Offers
                .Where(o => o.RestaurantId == restaurant.Id)
                .ToListAsync();

            return View("/Views/Restaurant/OfferManager/Index.cshtml", offers);
        }

        // ============================================================
        // CREATE — GET
        // ROUTE: /OfferManager/Create
        // ============================================================
        [HttpGet("/OfferManager/Create")]
        public async Task<IActionResult> Create()
        {
            var vm = new OfferFormViewModel
            {
                Offer = new Offer(),
                Categories = await _context.Categories.ToListAsync(),
                Tags = await _context.Tags.ToListAsync(),
                SelectedTags = new List<Guid>()
            };

            return View("/Views/Restaurant/OfferManager/Create.cshtml", vm);
        }

        // ============================================================
        // CREATE — POST
        // ============================================================
        [HttpPost("/OfferManager/Create")]
        public async Task<IActionResult> Create(OfferFormViewModel model, IFormFile? ImageUpload)
        {
            var user = await _userManager.GetUserAsync(User);
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

            if (restaurant == null)
            {
                TempData["Error"] = "Restaurant not found.";
                return RedirectToAction("Index");
            }

            // IMAGE UPLOAD
            if (ImageUpload != null)
            {
                var dir = Path.Combine("wwwroot", "images", "offers");
                Directory.CreateDirectory(dir);

                var filename = $"{Guid.NewGuid()}{Path.GetExtension(ImageUpload.FileName)}";
                var filePath = Path.Combine(dir, filename);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageUpload.CopyToAsync(stream);
                }

                model.Offer.ImageUrl = $"/images/offers/{filename}";
            }

            model.Offer.RestaurantId = restaurant.Id;

            _context.Offers.Add(model.Offer);
            await _context.SaveChangesAsync();

            // ADD TAGS
            if (model.SelectedTags != null)
            {
                foreach (var tagId in model.SelectedTags)
                {
                    _context.OfferTags.Add(new OfferTag
                    {
                        OfferId = model.Offer.Id,
                        TagId = tagId
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Offer created!";
            return RedirectToAction("Index");
        }

        // ============================================================
        // EDIT — GET
        // ROUTE: /OfferManager/Edit/{id}
        // ============================================================
        [HttpGet("/OfferManager/Edit/{id}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var offer = await _context.Offers
                .Include(o => o.OfferTags)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offer == null)
                return NotFound();

            var vm = new OfferFormViewModel
            {
                Offer = offer,
                Categories = await _context.Categories.ToListAsync(),
                Tags = await _context.Tags.ToListAsync(),
                SelectedTags = offer.OfferTags?.Select(t => t.TagId).ToList() ?? new List<Guid>()
            };

            return View("/Views/Restaurant/OfferManager/Edit.cshtml", vm);
        }

        // ============================================================
        // EDIT — POST
        // ============================================================
        [HttpPost("/OfferManager/Edit")]
        public async Task<IActionResult> Edit(OfferFormViewModel model, IFormFile? ImageUpload)
        {
            var offer = await _context.Offers
                .Include(o => o.OfferTags)
                .FirstOrDefaultAsync(o => o.Id == model.Offer.Id);

            if (offer == null)
                return NotFound();

            // UPDATE MAIN FIELDS
            offer.Title = model.Offer.Title;
            offer.Description = model.Offer.Description;
            offer.OriginalPrice = model.Offer.OriginalPrice;
            offer.DiscountedPrice = model.Offer.DiscountedPrice;
            offer.QuantityAvailable = model.Offer.QuantityAvailable;
            offer.CategoryId = model.Offer.CategoryId;
            offer.EndsAt = model.Offer.EndsAt;

            // IMAGE UPDATE
            if (ImageUpload != null)
            {
                var dir = Path.Combine("wwwroot", "images", "offers");
                Directory.CreateDirectory(dir);

                var filename = $"{Guid.NewGuid()}{Path.GetExtension(ImageUpload.FileName)}";
                var filePath = Path.Combine(dir, filename);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await ImageUpload.CopyToAsync(stream);

                offer.ImageUrl = $"/images/offers/{filename}";
            }

            // UPDATE TAGS
            _context.OfferTags.RemoveRange(offer.OfferTags);

            if (model.SelectedTags != null)
            {
                foreach (var tagId in model.SelectedTags)
                {
                    _context.OfferTags.Add(new OfferTag
                    {
                        OfferId = offer.Id,
                        TagId = tagId
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Offer updated!";
            return RedirectToAction("Index");
        }

        // ============================================================
        // DELETE — POST
        // ============================================================
        [HttpPost("/OfferManager/Delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var offer = await _context.Offers
                .Include(o => o.OfferTags)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offer == null)
            {
                TempData["Error"] = "Offer not found.";
                return RedirectToAction("Index");
            }

            _context.OfferTags.RemoveRange(offer.OfferTags);
            _context.Offers.Remove(offer);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Offer deleted!";
            return RedirectToAction("Index");
        }
    }
}