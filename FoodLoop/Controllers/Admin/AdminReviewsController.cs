using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodLoop.Data;
using FoodLoop.Models.ViewModels;

namespace FoodLoop.Controllers.Admin;

public class AdminReviewsController : AdminBaseController
{
    private const int PageSize = 10;

    public AdminReviewsController(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<IActionResult> Reviews(string? query, int page = 1)
    {
        var reviewsQuery = _context.Reviews
            .Include(r => r.Reservation)
                .ThenInclude(res => res.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            reviewsQuery = reviewsQuery.Where(r =>
                r.Comment.Contains(query));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            Info($"Резултати за търсене: {query}");
        }

        var totalItems = await reviewsQuery.CountAsync();

        var reviews = await reviewsQuery
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var pagination = new PaginationViewModel
        {
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize),
            Controller = "AdminReviews",
            Action = "Reviews",
            Query = query
        };

        ViewBag.Pagination = pagination;

        return AdminView("Reviews", reviews);
    }
}