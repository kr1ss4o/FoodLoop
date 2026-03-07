using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.ViewModels;
using FoodLoop.Services;
using FoodLoop.ViewModels.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers.Admin;

public class AdminRestaurantsController : AdminBaseController
{
    private const int PageSize = 10;

    private readonly UserManager<User> _userManager;
    private readonly AdminDeleteService _deleteService;

    public AdminRestaurantsController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        AdminDeleteService deleteService)
        : base(context)
    {
        _userManager = userManager;
        _deleteService = deleteService;
    }

    // =====================================================
    // LIST
    // =====================================================

    public async Task<IActionResult> Restaurants(string? query, int page = 1)
    {
        var restaurantsQuery = _context.Restaurants
            .Include(r => r.Owner)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            restaurantsQuery = restaurantsQuery.Where(r =>
                r.Name.Contains(query) ||
                r.BusinessEmail.Contains(query));
        }

        var totalItems = await restaurantsQuery.CountAsync();

        var restaurants = await restaurantsQuery
            .OrderBy(r => r.Name)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Pagination = new PaginationViewModel
        {
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize),
            Controller = "AdminRestaurants",
            Action = "Restaurants",
            Query = query
        };

        return AdminView("Restaurants", restaurants);
    }

    // =====================================================
    // DETAILS
    // =====================================================

    public async Task<IActionResult> Details(Guid id)
    {
        var restaurant = await _context.Restaurants
            .Include(r => r.Owner)
            .Include(r => r.Offers)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (restaurant == null)
            return NotFound();

        return AdminDetails(restaurant);
    }

    // =====================================================
    // CREATE
    // =====================================================

    [HttpGet]
    public IActionResult Create()
    {
        return AdminCreate(new AdminFormViewModel
        {
            Type = "Restaurant"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        AdminFormViewModel vm,
        IFormFile? ImageUpload,
        IFormFile? BannerUpload)
    {
        if (!ModelState.IsValid)
            return AdminCreate(vm);

        var user = new User
        {
            UserName = vm.OwnerEmail,
            Email = vm.OwnerEmail,
            FullName = vm.OwnerFullName,
            PhoneNumber = vm.OwnerPhone
        };

        var result = await _userManager.CreateAsync(user, vm.OwnerPassword);

        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Owner creation failed");
            return AdminCreate(vm);
        }

        await _userManager.AddToRoleAsync(user, "Restaurant");

        var restaurant = new Restaurant
        {
            Name = vm.RestaurantName,
            BusinessEmail = vm.BusinessEmail,
            Phone = vm.Phone,
            Address = vm.Address,
            OwnerUserId = user.Id
        };

        restaurant.ImageUrl = await SaveFile(ImageUpload) ?? vm.ImageUrl;
        restaurant.BannerImageUrl = await SaveFile(BannerUpload) ?? vm.BannerImageUrl;

        _context.Restaurants.Add(restaurant);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Restaurants));
    }

    // =====================================================
    // EDIT
    // =====================================================

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var restaurant = await _context.Restaurants
            .FirstOrDefaultAsync(r => r.Id == id);

        if (restaurant == null)
            return NotFound();

        var owner = await _userManager.FindByIdAsync(restaurant.OwnerUserId);

        var vm = new AdminFormViewModel
        {
            Type = "Restaurant",
            Id = restaurant.Id,

            RestaurantName = restaurant.Name,
            BusinessEmail = restaurant.BusinessEmail,
            Phone = restaurant.Phone,
            Address = restaurant.Address,

            ImageUrl = restaurant.ImageUrl,
            BannerImageUrl = restaurant.BannerImageUrl,

            OwnerFullName = owner?.FullName,
            OwnerEmail = owner?.Email,
            OwnerPhone = owner?.PhoneNumber
        };

        return AdminEdit(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        AdminFormViewModel vm,
        IFormFile? ImageUpload,
        IFormFile? BannerUpload)
    {
        if (vm.Id == null)
            return BadRequest();

        var restaurant = await _context.Restaurants
            .Include(r => r.Owner)
            .FirstOrDefaultAsync(r => r.Id == vm.Id);

        if (restaurant == null)
            return NotFound();

        // ======================
        // BUSINESS
        // ======================

        restaurant.Name = vm.RestaurantName;
        restaurant.BusinessEmail = vm.BusinessEmail;
        restaurant.Phone = vm.Phone;
        restaurant.Address = vm.Address;

        // Owner

        var owner = await _userManager.FindByIdAsync(restaurant.OwnerUserId);

        if (owner != null)
        {
            owner.FullName = vm.OwnerFullName;
            owner.PhoneNumber = vm.OwnerPhone;

            if (!string.IsNullOrWhiteSpace(vm.OwnerEmail) && owner.Email != vm.OwnerEmail)
            {
                owner.Email = vm.OwnerEmail;
                owner.UserName = vm.OwnerEmail;
            }

            var result = await _userManager.UpdateAsync(owner);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Owner update failed");
                return AdminEdit(vm);
            }
        }

        // ======================
        // IMAGES
        // ======================

        var imagePath = await SaveFile(ImageUpload);
        if (imagePath != null)
            restaurant.ImageUrl = imagePath;
        else if (!string.IsNullOrWhiteSpace(vm.ImageUrl))
            restaurant.ImageUrl = vm.ImageUrl;

        var bannerPath = await SaveFile(BannerUpload);
        if (bannerPath != null)
            restaurant.BannerImageUrl = bannerPath;
        else if (!string.IsNullOrWhiteSpace(vm.BannerImageUrl))
            restaurant.BannerImageUrl = vm.BannerImageUrl;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = restaurant.Id });
    }

    // =====================================================
    // DELETE
    // =====================================================

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _deleteService.DeleteRestaurantAsync(id);
        return RedirectToAction(nameof(Restaurants));
    }

    // =====================================================
    // FILE UPLOAD HELPER
    // =====================================================

    private async Task<string?> SaveFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return null;

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine("wwwroot/uploads", fileName);

        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);

        return "/uploads/" + fileName;
    }
}