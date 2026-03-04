using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.ViewModels;

namespace FoodLoop.Controllers.Admin;

public class AdminRestaurantsController : AdminBaseController
{
    private const int PageSize = 10;

    public AdminRestaurantsController(ApplicationDbContext context)
        : base(context)
    {
    }

    // LIST
    public async Task<IActionResult> Restaurants(string? query, int page = 1)
    {
        var restaurantsQuery = _context.Restaurants
            .Include(r => r.Owner)
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

    // DETAILS
    public async Task<IActionResult> Details(Guid id)
    {
        var restaurant = await _context.Restaurants
            .Include(r => r.Owner)
            .Include(r => r.Offers)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (restaurant == null)
            return NotFound();

        return AdminDetails(restaurant);
    }

    // CREATE GET
    public IActionResult Create()
    {
        return AdminCreate(new Restaurant());
    }

    // CREATE POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Restaurant model)
    {
        if (!ModelState.IsValid)
            return AdminCreate(model);

        _context.Restaurants.Add(model);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Restaurants));
    }

    // EDIT GET
    public async Task<IActionResult> Edit(Guid id)
    {
        var restaurant = await _context.Restaurants
            .FirstOrDefaultAsync(r => r.Id == id);

        if (restaurant == null)
            return NotFound();

        return AdminEdit(restaurant);
    }

    // EDIT POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Restaurant input)
    {
        var restaurant = await _context.Restaurants
            .FirstOrDefaultAsync(r => r.Id == input.Id);

        if (restaurant == null)
            return NotFound();

        restaurant.Name = input.Name;
        restaurant.BusinessEmail = input.BusinessEmail;
        restaurant.Phone = input.Phone;
        restaurant.Address = input.Address;
        restaurant.ImageUrl = input.ImageUrl;
        restaurant.BannerImageUrl = input.BannerImageUrl;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = restaurant.Id });
    }

    // DELETE
    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var restaurant = await _context.Restaurants.FindAsync(id);

        if (restaurant == null)
            return NotFound();

        _context.Restaurants.Remove(restaurant);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Restaurants));
    }
}