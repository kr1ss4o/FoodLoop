using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodLoop.Data;
using FoodLoop.Models.ViewModels;

namespace FoodLoop.Controllers.Admin;

public class AdminReservationsController : AdminBaseController
{
    private const int PageSize = 10;

    public AdminReservationsController(ApplicationDbContext context)
        : base(context)
    {
    }

    // =====================================================
    // LIST
    // =====================================================

    public async Task<IActionResult> Reservations(string? query, int page = 1)
    {
        var reservationsQuery = _context.Reservations
            .Include(r => r.User)
            .Include(r => r.Items)
                .ThenInclude(i => i.Offer)
                    .ThenInclude(o => o.Restaurant)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            reservationsQuery = reservationsQuery.Where(r =>
                r.User.Email.Contains(query));
        }

        var totalItems = await reservationsQuery.CountAsync();

        var reservations = await reservationsQuery
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var pagination = new PaginationViewModel
        {
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize),
            Controller = "AdminReservations",
            Action = "Reservations",
            Query = query
        };

        ViewBag.Pagination = pagination;

        return AdminView("Reservations", reservations);
    }

    // =====================================================
    // DETAILS
    // =====================================================

    public async Task<IActionResult> Details(Guid id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.User)
            .Include(r => r.Items)
                .ThenInclude(i => i.Offer)
                    .ThenInclude(o => o.Restaurant)
            .Include(r => r.Review)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
            return NotFound();

        return AdminDetails(reservation);
    }
}