using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public ProfileController(UserManager<User> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Fetch total orders
            var totalOrders = await _context.Reservations
                .Where(r => r.UserId == user.Id)
                .CountAsync();

            // Total money spent
            var moneySpent = await _context.Reservations
                .Where(r => r.UserId == user.Id)
                .Include(r => r.Offer)
                .SumAsync(r => (decimal?)r.Offer.DiscountedPrice) ?? 0;

            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber ?? "",
                AccountCreated = user.CreatedAt,
                TotalOrders = totalOrders,
                MoneySpent = moneySpent,
                ProfileImageUrl = user.ProfileImageUrl
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(string FullName, string PhoneNumber,
        string CurrentPassword, string? NewPassword, IFormFile? ProfileImage)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            // Validate phone length 10-13
            if (PhoneNumber.Length < 10 || PhoneNumber.Length > 13)
            {
                TempData["Error"] = "Phone number must be between 10 and 13 digits.";
                return RedirectToAction("Index");
            }

            // Email cannot be changed, but Phone must be unique
            var existingPhone = await _context.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == PhoneNumber && u.Id != user.Id);

            if (existingPhone != null)
            {
                TempData["Error"] = "Phone number is already in use.";
                return RedirectToAction("Index");
            }

            // Check current password
            var passwordCheck = await _userManager.CheckPasswordAsync(user, CurrentPassword);

            if (!passwordCheck)
            {
                TempData["Error"] = "Incorrect current password.";
                return RedirectToAction("Index");
            }

            // Update name + phone
            user.FullName = FullName;
            user.PhoneNumber = PhoneNumber;

            // Update photo
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

            // Change password
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