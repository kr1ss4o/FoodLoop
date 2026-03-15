using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.Enums;
using FoodLoop.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers
{
    [Authorize(Roles = "Restaurant")]
    public class BusinessProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _environment;

        public BusinessProfileController(ApplicationDbContext context, UserManager<User> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                TempData["Error"] = "Не беше намерен човек.";
                return RedirectToAction("Login", "Account");
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

            if (restaurant == null)
            {
                TempData["Error"] = "Не  беше намерен ресторант.";
                return RedirectToAction("Index", "Home");
            }

            // =========================
            // GET OFFER IDS
            // =========================

            var offerIds = await _context.Offers
                .Where(o => o.RestaurantId == restaurant.Id)
                .Select(o => o.Id)
                .ToListAsync();

            // =========================
            // GET RESERVATION IDS
            // =========================

            var reservationIds = await _context.ReservationItems
                .Where(ri => offerIds.Contains(ri.OfferId))
                .Select(ri => ri.ReservationId)
                .Distinct()
                .ToListAsync();

            // =========================
            // REVIEW STATS
            // =========================

            var reviewStats = await _context.Reviews
                .Where(r => reservationIds.Contains(r.ReservationId))
                .GroupBy(r => 1)
                .Select(g => new
                {
                    Count = g.Count(),
                    Average = g.Average(r => r.Rating)
                })
                .FirstOrDefaultAsync();

            int reviewCount = reviewStats?.Count ?? 0;
            double averageRating = reviewStats?.Average ?? 0;

            // =========================
            // ACTIVE OFFERS
            // =========================

            int activeOffers = await _context.Offers
                .CountAsync(o =>
                    o.RestaurantId == restaurant.Id &&
                    o.EndsAt > DateTime.UtcNow &&
                    o.QuantityAvailable > 0);

            // =========================
            // ALL FINISHED RESERVATIONS
            // =========================

            int reservations = await _context.Reservations
                .Where(r =>
                    r.Status == ReservationStatus.Finished &&
                    r.Items.Any(i => offerIds.Contains(i.OfferId)))
                .CountAsync();

            // =========================
            // VIEW MODEL
            // =========================

            var vm = new BusinessProfileViewModel
            {
                RestaurantName = restaurant.Name,
                BusinessEmail = restaurant.BusinessEmail,
                Phone = restaurant.Phone,
                Address = restaurant.Address,
                ImageUrl = restaurant.ImageUrl,
                BannerImageUrl = restaurant.BannerImageUrl,

                OwnerName = user.FullName,
                OwnerEmail = user.Email,
                OwnerPhone = user.PhoneNumber ?? "",
                AccountCreated = user.CreatedAt,

                AverageRating = averageRating,
                ReviewCount = reviewCount,
                ActiveOffers = activeOffers,
                TotalReservations = reservations,

                EditProfileModal = new EditProfileViewModel
                {
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber ?? "",
                    BusinessPhone = restaurant.Phone,
                    IsRestaurant = true,

                    RestaurantName = restaurant.Name,
                    BusinessEmail = restaurant.BusinessEmail,
                    Address = restaurant.Address,

                    ProfileImageUrl = restaurant.ImageUrl,
                    BannerImageUrl = restaurant.BannerImageUrl
                }
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model, IFormFile? ProfileImage, IFormFile? BannerImage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

            if (restaurant == null)
                return RedirectToAction("Index");

            // PASSWORD CHECK
            var passwordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);

            if (!passwordValid)
            {
                TempData["Error"] = "Грешна парола. Промените не бяха запазени.";
                return RedirectToAction("Index");
            }

            // RESTAURANT INFO
            restaurant.Name = model.RestaurantName ?? restaurant.Name;
            restaurant.BusinessEmail = model.BusinessEmail ?? restaurant.BusinessEmail;
            restaurant.Address = model.Address ?? restaurant.Address;
            if (!string.IsNullOrWhiteSpace(model.BusinessPhone))
            {
                restaurant.Phone = model.BusinessPhone;
            }

            // PROFILE IMAGE URL
            if (!string.IsNullOrWhiteSpace(model.ProfileImageUrl))
            {
                restaurant.ImageUrl = model.ProfileImageUrl.Trim();
            }

            // PROFILE IMAGE FILE
            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "restaurants");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ProfileImage.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await ProfileImage.CopyToAsync(stream);

                restaurant.ImageUrl = $"/images/restaurants/{fileName}";
            }

            // BANNER URL
            if (!string.IsNullOrWhiteSpace(model.BannerImageUrl))
            {
                restaurant.BannerImageUrl = model.BannerImageUrl.Trim();
            }

            // BANNER FILE
            if (BannerImage != null && BannerImage.Length > 0)
            {
                var folder = Path.Combine(_environment.WebRootPath, "images", "banners");
                Directory.CreateDirectory(folder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(BannerImage.FileName)}";
                var path = Path.Combine(folder, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await BannerImage.CopyToAsync(stream);

                restaurant.BannerImageUrl = $"/images/banners/{fileName}";
            }

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Профилът Ви бе редактиран успешно.";

            return RedirectToAction("Index");
        }
    }
}