using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // LIST
        public async Task<IActionResult> Index()
        {
            var restaurants = await _context.Restaurants
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.BusinessEmail,
                    r.Phone,
                    r.Address,
                    r.OwnerUserId
                })
                .ToListAsync();

            return View(restaurants);
        }

        // CREATE - GET
        [HttpGet]
        public IActionResult Create() => View(new CreateRestaurantViewModel());

        // CREATE - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRestaurantViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // 1) prevent duplicate user
            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Email already exists.");
                return View(model);
            }

            // 2) create user (restaurant owner)
            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.OwnerFullName
            };

            var createUserResult = await _userManager.CreateAsync(user, model.Password);
            if (!createUserResult.Succeeded)
            {
                foreach (var err in createUserResult.Errors)
                    ModelState.AddModelError("", err.Description);

                return View(model);
            }

            // 3) assign role Restaurant
            await _userManager.AddToRoleAsync(user, "Restaurant");

            // 4) create Restaurant entity and link OwnerUserId
            var restaurant = new Restaurant
            {
                Name = model.RestaurantName,
                BusinessEmail = model.BusinessEmail,
                Phone = model.Phone,
                Address = model.Address,
                ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim(),
                OwnerUserId = user.Id,
                Rating = 0
            };

            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Restaurant profile created.";
            return RedirectToAction(nameof(Index));
        }
    }
}