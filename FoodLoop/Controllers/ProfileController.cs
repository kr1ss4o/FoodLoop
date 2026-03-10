using FoodLoop.Data;
using FoodLoop.Models.DTOs;
using FoodLoop.Models.Entities;
using FoodLoop.Models.Enums;
using FoodLoop.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers
{
    [Authorize (Roles="Client, Admin")]
    public class ProfileController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProfileController(UserManager<User> userManager, ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index(int reviewsPage = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // =========================
            // OLD CALCULATIONS (НЕ ГИ ПИПАМЕ)
            // =========================
            var totalOrders = await _context.Reservations
                .Where(r => r.UserId == user.Id &&
                       r.Status != ReservationStatus.Canceled)
                .CountAsync();

            var moneySpent = await _context.Reservations
                .Where(r => r.UserId == user.Id &&
                       r.Status == ReservationStatus.Finished)
                .SumAsync(r => (decimal?)r.TotalPrice) ?? 0;

            // =========================
            // LAST 3 ORDERS (DTO version)
            // =========================
            var recentOrders = await _context.Reservations
            .AsNoTracking()
            .Where(r => r.UserId == user.Id)
            .Include(r => r.Items)
                .ThenInclude(i => i.Offer)
                    .ThenInclude(o => o.Restaurant)
                .OrderByDescending(r => r.CreatedAt)
            .Take(3)
            .Select(r => new ReservationSummaryDto
            {
                Id = r.Id,
                CreatedAt = r.CreatedAt,
                TotalPrice = r.TotalPrice,
                DeliveryType = r.DeliveryType,
                Status = r.Status.ToString(),

                // взимаме ресторанта от първия артикул
                RestaurantName = r.Items
                .Select(i => i.Offer.Restaurant.Name)
                .FirstOrDefault() ?? "",

                Items = r.Items.Select(i => new ReservationItemDto
                {
                    OfferTitle = i.Offer.Title,
                    Quantity = i.Quantity
                }).ToList()
            })
            .ToListAsync();

            // =========================
            // REVIEWS (3 per page)
            // =========================
            const int pageSize = 3;
            if (reviewsPage < 1) reviewsPage = 1;

            var reviewsQuery = _context.Reviews
                .AsNoTracking()
                .Where(r => r.Reservation.UserId == user.Id)
                .OrderByDescending(r => r.CreatedAt);

            var totalReviews = await reviewsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalReviews / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (reviewsPage > totalPages) reviewsPage = totalPages;

            var reviews = await reviewsQuery
                .Skip((reviewsPage - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new UserReviewDto
                {
                    ReviewId = r.Id,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    RestaurantName = r.Reservation.Items
                        .Select(i => i.Offer.Restaurant.Name)
                        .FirstOrDefault()!
                })
                .ToListAsync();

            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber ?? "",
                AccountCreated = user.CreatedAt,
                TotalOrders = totalOrders,
                MoneySpent = moneySpent,
                ProfileImageUrl = user.ProfileImageUrl,

                RecentOrders = recentOrders,
                Reviews = reviews,
                ReviewsPage = reviewsPage,
                TotalReviewPages = totalPages,

                IsRestaurant = false,

                // Edit profile modal
                EditProfileModal = new EditProfileViewModel
                {
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber ?? "",
                    IsRestaurant = false,

                    // ако искаш да показва текущата снимка в preview
                    ProfileImageUrl = user.ProfileImageUrl
                }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model, IFormFile? ProfileImage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return RedirectToAction("Index");

            // Update basic info
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            // === IMAGE URL (if provided) ===
            if (!string.IsNullOrWhiteSpace(model.ProfileImageUrl))
            {
                user.ProfileImageUrl = model.ProfileImageUrl.Trim();
            }

            // Wrong password toast
            var passwordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);

            if (!passwordValid)
            {
                TempData["Error"] = "Wrong password. Could not save the changes.";
                return RedirectToAction("Index");
            }

            // === FILE UPLOAD (if provided) ===
            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "users");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ProfileImage.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(stream);
                }

                user.ProfileImageUrl = $"/images/users/{fileName}";
            }

            await _userManager.UpdateAsync(user);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}