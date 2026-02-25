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
                AccountCreated = restaurant.Owner.CreatedAt,


                // Edit profile modal
                EditProfileModal = new EditProfileViewModel
                {
                    FullName = restaurant.Owner.FullName,
                    PhoneNumber = restaurant.Owner.PhoneNumber ?? "",
                    IsRestaurant = true,

                    RestaurantName = restaurant.Name,
                    BusinessEmail = restaurant.BusinessEmail,
                    Address = restaurant.Address,

                    ProfileImageUrl = restaurant.ImageUrl
                }
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model, IFormFile? ProfileImage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var restaurant = await _context.Restaurants
            .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

            if (restaurant == null)
                return RedirectToAction("Index");

            // Update owner info
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            // Update restaurant info
            restaurant.Name = model.RestaurantName ?? restaurant.Name;
            restaurant.BusinessEmail = model.BusinessEmail ?? restaurant.BusinessEmail;
            restaurant.Address = model.Address ?? restaurant.Address;

            // Wrong password toast
            var passwordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);

            if (!passwordValid)
            {
                TempData["Error"] = "Wrong password. Could not save the changes.";
                return RedirectToAction("Index");
            }

            // === IMAGE URL ===
            if (!string.IsNullOrWhiteSpace(model.ProfileImageUrl))
            {
                restaurant.ImageUrl = model.ProfileImageUrl.Trim();
            }

            // === FILE UPLOAD ===
            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "restaurants");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ProfileImage.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(stream);
                }

                restaurant.ImageUrl = $"/images/restaurants/{fileName}";
            }

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}