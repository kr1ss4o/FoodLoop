using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.ViewModels;
using FoodLoop.Services;
using FoodLoop.ViewModels.Admin;

namespace FoodLoop.Controllers.Admin;

public class AdminOffersController : AdminBaseController
{
    private const int PageSize = 10;
    private readonly AdminDeleteService _deleteService;

    public AdminOffersController(
        ApplicationDbContext context,
        AdminDeleteService deleteService)
        : base(context)
    {
        _deleteService = deleteService;
    }

    // =====================================================
    // LIST
    // =====================================================

    public async Task<IActionResult> Offers(string? query, int page = 1)
    {
        var offersQuery = _context.Offers
            .Include(o => o.Restaurant)
            .Include(o => o.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            offersQuery = offersQuery.Where(o =>
                o.Title.Contains(query) ||
                o.Restaurant.Name.Contains(query));
        }

        var totalItems = await offersQuery.CountAsync();

        var offers = await offersQuery
            .OrderByDescending(o => o.EndsAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Pagination = new PaginationViewModel
        {
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize),
            Controller = "AdminOffers",
            Action = "Offers",
            Query = query
        };

        return AdminView("Offers", offers);
    }

    // =====================================================
    // DETAILS
    // =====================================================

    public async Task<IActionResult> Details(Guid id)
    {
        var offer = await _context.Offers
            .Include(o => o.Restaurant)
            .Include(o => o.Category)
            .Include(o => o.OfferTags)
                .ThenInclude(t => t.Tag)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (offer == null)
            return NotFound();

        return AdminDetails(offer);
    }

    // =====================================================
    // CREATE
    // =====================================================

    public IActionResult Create()
    {
        var vm = new AdminFormViewModel
        {
            Type = "Offer",
            Restaurants = _context.Restaurants.ToList(),
            Categories = _context.Categories.ToList(),
            AllTags = _context.Tags.ToList()
        };

        return AdminCreate(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        AdminFormViewModel vm,
        IFormFile? ImageUpload)
    {

        if (!ModelState.IsValid)
        {
            vm.Restaurants = await _context.Restaurants.ToListAsync();
            vm.Categories = await _context.Categories.ToListAsync();
            vm.AllTags = await _context.Tags.ToListAsync();

            return AdminCreate(vm);
        }

        var offer = new Offer
        {
            Title = vm.Title,
            Description = vm.Description,
            OriginalPrice = vm.OriginalPrice ?? 0,
            DiscountedPrice = vm.DiscountedPrice ?? 0,
            QuantityAvailable = vm.QuantityAvailable ?? 0,
            EndsAt = vm.EndsAt ?? DateTime.UtcNow.AddHours(1),
            CategoryId = vm.CategoryId ?? Guid.Empty,
            RestaurantId = vm.RestaurantId ?? Guid.Empty
        };

        var imagePath = await SaveFile(ImageUpload);
        offer.ImageUrl = imagePath ?? vm.ImageUrl;

        _context.Offers.Add(offer);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Offers));
    }

    // =====================================================
    // EDIT
    // =====================================================

    public async Task<IActionResult> Edit(Guid id)
    {
        var offer = await _context.Offers
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        if (offer == null)
            return NotFound();

        var vm = new AdminFormViewModel
        {
            Type = "Offer",
            Id = offer.Id,
            Title = offer.Title,
            Description = offer.Description,
            OriginalPrice = offer.OriginalPrice,
            DiscountedPrice = offer.DiscountedPrice,
            QuantityAvailable = offer.QuantityAvailable,
            EndsAt = offer.EndsAt,
            CategoryId = offer.CategoryId,
            RestaurantId = offer.RestaurantId,
            ImageUrl = offer.ImageUrl,

            Restaurants = await _context.Restaurants.ToListAsync(),
            Categories = await _context.Categories.ToListAsync(),
            AllTags = await _context.Tags.ToListAsync()
        };

        return AdminEdit(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        AdminFormViewModel vm,
        IFormFile? ImageUpload)
    {

        if (!ModelState.IsValid)
        {
            vm.Restaurants = await _context.Restaurants.ToListAsync();
            vm.Categories = await _context.Categories.ToListAsync();
            vm.AllTags = await _context.Tags.ToListAsync();

            return AdminEdit(vm);
        }

        if (vm.Id == null)
            return BadRequest();

        var offer = await _context.Offers
            .FirstOrDefaultAsync(o => o.Id == vm.Id);

        if (offer == null)
            return NotFound();

        offer.Title = vm.Title;
        offer.Description = vm.Description;
        offer.OriginalPrice = vm.OriginalPrice ?? offer.OriginalPrice;
        offer.DiscountedPrice = vm.DiscountedPrice ?? offer.DiscountedPrice;
        offer.QuantityAvailable = vm.QuantityAvailable ?? offer.QuantityAvailable;
        offer.EndsAt = vm.EndsAt ?? offer.EndsAt;
        offer.CategoryId = vm.CategoryId ?? offer.CategoryId;
        offer.RestaurantId = vm.RestaurantId ?? offer.RestaurantId;

        var imagePath = await SaveFile(ImageUpload);

        if (imagePath != null)
            offer.ImageUrl = imagePath;
        else if (!string.IsNullOrWhiteSpace(vm.ImageUrl))
            offer.ImageUrl = vm.ImageUrl;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = offer.Id });
    }

    // =====================================================
    // DELETE
    // =====================================================

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _deleteService.DeleteOfferAsync(id);
        return RedirectToAction(nameof(Offers));
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