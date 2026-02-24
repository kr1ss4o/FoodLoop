using FoodLoop.Data;
using FoodLoop.Models.DTOs;
using FoodLoop.Models.Entities;
using FoodLoop.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Restaurant")]
public class RestaurantReviewsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public RestaurantReviewsController(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(
        int? rating,
        string? search,
        string? sort,
        int page = 1)
    {
        const int pageSize = 10;

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var restaurant = await _context.Restaurants
            .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

        if (restaurant == null)
            return RedirectToAction("Index", "RestaurantDashboard");

        var query = _context.Reviews
            .Where(rv =>
                rv.Reservation.Status == ReservationStatus.Finished &&
                rv.Reservation.Items.Any(i => i.Offer.RestaurantId == restaurant.Id))
            .AsQueryable();

        // Filter by rating
        if (rating.HasValue)
            query = query.Where(r => r.Rating == rating.Value);

        // Search
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r =>
                r.Comment!.Contains(search) ||
                r.Reservation.User.FullName.Contains(search));

        // Sorting
        query = sort switch
        {
            "rating_asc" => query.OrderBy(r => r.Rating),
            "rating_desc" => query.OrderByDescending(r => r.Rating),
            "oldest" => query.OrderBy(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(rv => new RestaurantReviewDto
            {
                ReviewId = rv.Id,
                ReservationId = rv.ReservationId,
                Rating = rv.Rating,
                Comment = rv.Comment,
                AuthorName = rv.Reservation.User.FullName,
                CreatedAt = rv.CreatedAt
            })
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        ViewBag.Rating = rating;
        ViewBag.Search = search;
        ViewBag.Sort = sort;

        return View(reviews);
    }
}