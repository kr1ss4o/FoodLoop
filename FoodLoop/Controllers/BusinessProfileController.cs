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
    public class BusinessProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public BusinessProfileController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Login", "Account");
            }

            var restaurant = await _context.Restaurants
                .Include(r => r.Owner)
                .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

            if (restaurant == null)
            {
                TempData["Error"] = "Restaurant not found.";
                return RedirectToAction("Index", "Home");
            }

            if (restaurant.Owner == null)
            {
                // TEMP FIX to avoid crash
                restaurant.Owner = user;
            }

            var vm = new BusinessProfileViewModel
            {
                RestaurantName = restaurant.Name,
                BusinessEmail = restaurant.BusinessEmail,
                Phone = restaurant.Phone,
                Address = restaurant.Address,
                ImageUrl = restaurant.ImageUrl,

                OwnerName = restaurant.Owner.FullName,
                OwnerEmail = restaurant.Owner.Email,
                OwnerPhone = restaurant.Owner.PhoneNumber ?? "",
                AccountCreated = restaurant.Owner.CreatedAt
            };

            return View(vm);
        }
        public async Task<IActionResult> OrderDashboard()
        {
            var user = await _userManager.GetUserAsync(User);

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

            var reservations = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Offer)
                .Where(r => r.Offer.RestaurantId == restaurant.Id)
                .ToListAsync();

            return View("~/Views/Restaurant/OrderDashboard/Index.cshtml", reservations);
        }
        [HttpPost]
        public async Task<IActionResult> EditProfile( string FullName, string PhoneNumber, string RestaurantName, string BusinessEmail, string Address, string CurrentPassword, string? NewPassword, IFormFile? ProfileImage)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

            if (restaurant == null)
            {
                TempData["Error"] = "Restaurant not found.";
                return RedirectToAction("Index");
            }

            // ========== PASSWORD CHECK ==========
            var passwordCheck = await _userManager.CheckPasswordAsync(user, CurrentPassword);
            if (!passwordCheck)
            {
                TempData["Error"] = "Incorrect current password.";
                return RedirectToAction("Index");
            }

            // ========== UPDATE OWNER (USER) ==========
            user.FullName = FullName;
            user.PhoneNumber = PhoneNumber;

            if (!string.IsNullOrWhiteSpace(NewPassword))
            {
                var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
                if (!result.Succeeded)
                {
                    TempData["Error"] = "New password is invalid.";
                    return RedirectToAction("Index");
                }
            }

            // ========== UPDATE RESTAURANT ==========
            restaurant.Name = RestaurantName;
            restaurant.BusinessEmail = BusinessEmail;
            restaurant.Phone = PhoneNumber;
            restaurant.Address = Address;

            // ========== IMAGE UPDATE ==========
            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                var dir = Path.Combine("wwwroot", "images", "restaurants");
                Directory.CreateDirectory(dir);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ProfileImage.FileName)}";
                var filePath = Path.Combine(dir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(stream);
                }

                restaurant.ImageUrl = $"/images/restaurants/{fileName}";
            }

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile successfully updated!";
            return RedirectToAction("Index");
        }

    }
}