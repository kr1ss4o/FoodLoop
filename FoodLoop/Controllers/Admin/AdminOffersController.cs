using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.ViewModels;
using FoodLoop.Services;

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

        var pagination = new PaginationViewModel
        {
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize),
            Controller = "AdminOffers",
            Action = "Offers",
            Query = query
        };

        ViewBag.Pagination = pagination;

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
        return AdminCreate(new Offer());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Offer model)
    {
        if (!ModelState.IsValid)
            return AdminCreate(model);

        _context.Offers.Add(model);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Offers));
    }

    // =====================================================
    // EDIT
    // =====================================================

    public async Task<IActionResult> Edit(Guid id)
    {
        var offer = await _context.Offers
            .FirstOrDefaultAsync(o => o.Id == id);

        if (offer == null)
            return NotFound();

        return AdminEdit(offer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Offer input)
    {
        var offer = await _context.Offers
            .FirstOrDefaultAsync(o => o.Id == input.Id);

        if (offer == null)
            return NotFound();

        offer.Title = input.Title;
        offer.Description = input.Description;
        offer.OriginalPrice = input.OriginalPrice;
        offer.DiscountedPrice = input.DiscountedPrice;
        offer.QuantityAvailable = input.QuantityAvailable;
        offer.EndsAt = input.EndsAt;
        offer.ImageUrl = input.ImageUrl;
        offer.CategoryId = input.CategoryId;
        offer.RestaurantId = input.RestaurantId;

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
}