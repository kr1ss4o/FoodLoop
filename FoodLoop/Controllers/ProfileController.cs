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

        public ProfileController(UserManager<User> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
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
                .OrderByDescending(r => r.CreatedAt)
                .Take(3)
                .Select(r => new ReservationSummaryDto
                {
                    Id = r.Id,
                    CreatedAt = r.CreatedAt,
                    TotalPrice = r.TotalPrice,
                    DeliveryType = r.DeliveryType,
                    Status = r.Status.ToString(),
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
                TotalReviewPages = totalPages
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(
            string FullName,
            string PhoneNumber,
            string CurrentPassword,
            string? NewPassword,
            IFormFile? ProfileImage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (PhoneNumber.Length < 10 || PhoneNumber.Length > 13)
            {
                TempData["Error"] = "Phone number must be between 10 and 13 digits.";
                return RedirectToAction("Index");
            }

            var existingPhone = await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == PhoneNumber && u.Id != user.Id);

            if (existingPhone != null)
            {
                TempData["Error"] = "Phone number is already in use.";
                return RedirectToAction("Index");
            }

            var passwordCheck = await _userManager.CheckPasswordAsync(user, CurrentPassword);

            if (!passwordCheck)
            {
                TempData["Error"] = "Incorrect current password.";
                return RedirectToAction("Index");
            }

            user.FullName = FullName;
            user.PhoneNumber = PhoneNumber;

            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                var uploadDir = Path.Combine("wwwroot", "images", "profile");

                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ProfileImage.FileName)}";
                var filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(stream);
                }

                user.ProfileImageUrl = $"/images/profile/{fileName}";
            }

            if (!string.IsNullOrEmpty(NewPassword))
            {
                var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);

                if (!result.Succeeded)
                {
                    TempData["Error"] = "New password is invalid.";
                    return RedirectToAction("Index");
                }
            }

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile successfully updated!";
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