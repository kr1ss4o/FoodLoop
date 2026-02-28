using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.ViewModels.Admin;
using FoodLoop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AdminDeleteService _deleteService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public AdminController(
            AdminDeleteService deleteService,
            ApplicationDbContext context,
            UserManager<User> userManager)
        {
            _deleteService = deleteService;
            _context = context;
            _userManager = userManager;
        }

        // ================== INDEX ==================

        public async Task<IActionResult> Index()
        {
            var restaurants = await _context.Restaurants
                .AsNoTracking()
                .ToListAsync();

            return View(restaurants);
        }

        // ================== CREATE ==================

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRestaurantViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.OwnerFullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Restaurant");

            var restaurant = new Restaurant
            {
                Name = model.RestaurantName,
                BusinessEmail = model.BusinessEmail,
                Phone = model.Phone,
                Address = model.Address,
                ImageUrl = model.ImageUrl,
                OwnerUserId = user.Id
            };

            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ================== EDIT ==================

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurant == null)
                return NotFound();

            var vm = new EditRestaurantViewModel
            {
                Id = restaurant.Id,
                Name = restaurant.Name,
                BusinessEmail = restaurant.BusinessEmail,
                Phone = restaurant.Phone,
                Address = restaurant.Address,
                ImageUrl = restaurant.ImageUrl
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditRestaurantViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == model.Id);

            if (restaurant == null)
                return NotFound();

            restaurant.Name = model.Name;
            restaurant.BusinessEmail = model.BusinessEmail;
            restaurant.Phone = model.Phone;
            restaurant.Address = model.Address;
            restaurant.ImageUrl = model.ImageUrl;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ================== DELETE ==================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRestaurant(Guid id)
        {
            await _deleteService.DeleteRestaurantAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOffer(Guid id)
        {
            await _deleteService.DeleteOfferAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            await _deleteService.DeleteReservationAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            await _deleteService.DeleteReviewAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}