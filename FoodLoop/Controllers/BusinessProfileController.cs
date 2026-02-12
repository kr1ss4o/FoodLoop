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
        public async Task<IActionResult> EditProfile(string RestaurantName, string BusinessEmail,
    string Phone, string Address, IFormFile? ImageUpload)
        {
            var user = await _userManager.GetUserAsync(User);

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

            if (restaurant == null)
            {
                TempData["Error"] = "Restaurant not found.";
                return RedirectToAction("Index");
            }

            restaurant.Name = RestaurantName;
            restaurant.BusinessEmail = BusinessEmail;
            restaurant.Phone = Phone;
            restaurant.Address = Address;

            if (ImageUpload != null && ImageUpload.Length > 0)
            {
                var dir = Path.Combine("wwwroot", "images", "restaurants");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(ImageUpload.FileName)}";
                var filePath = Path.Combine(dir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageUpload.CopyToAsync(stream);
                }

                restaurant.ImageUrl = $"/images/restaurants/{fileName}";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Business profile updated!";
            return RedirectToAction("Index");
        }

    }
}