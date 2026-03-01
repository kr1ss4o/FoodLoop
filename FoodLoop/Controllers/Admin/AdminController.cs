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

        // ================== DASHBOARD ==================

        public async Task<IActionResult> Dashboard()
        {
            var stats = new
            {
                Restaurants = await _context.Restaurants.CountAsync(),
                Offers = await _context.Offers.CountAsync(),
                Reservations = await _context.Reservations.CountAsync(),
                Reviews = await _context.Reviews.CountAsync()
            };

            return View(stats);
        }

        // ================== OFFERS ==================

        public async Task<IActionResult> Offers(string? search, int page = 1)
        {
            const int pageSize = 10;

            var query = _context.Offers
                .Include(o => o.Restaurant)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(o =>
                    o.Title.Contains(search) ||
                    o.Restaurant.Name.Contains(search));
            }

            var totalItems = await query.CountAsync();

            var offers = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Search = search;

            return View(offers);
        }

        public async Task<IActionResult> Reservations(string? search, int page = 1)
        {
            const int pageSize = 10;

            var query = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Items)
                    .ThenInclude(i => i.Offer)
                        .ThenInclude(o => o.Restaurant)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r =>
                    r.User.Email.Contains(search) ||
                    r.Items.Any(i =>
                        i.Offer.Restaurant.Name.Contains(search)));
            }

            var totalItems = await query.CountAsync();

            var reservations = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Search = search;

            return View(reservations);
        }

        public async Task<IActionResult> Reviews(string? search, int page = 1)
        {
            const int pageSize = 10;

            var query = _context.Reviews
                .Include(r => r.Reservation)
                    .ThenInclude(res => res.Items)
                        .ThenInclude(i => i.Offer)
                            .ThenInclude(o => o.Restaurant)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r =>
                    (r.Comment != null && r.Comment.Contains(search)) ||
                    r.Reservation.Items.Any(i =>
                        i.Offer.Restaurant.Name.Contains(search)));
            }

            var totalItems = await query.CountAsync();

            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Search = search;

            return View(reviews);
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