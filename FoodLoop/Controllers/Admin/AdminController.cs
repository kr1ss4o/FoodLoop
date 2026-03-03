using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FoodLoop.Data;
using FoodLoop.Models.Entities;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private const int pageSize = 10;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    // =====================================================
    // DASHBOARD
    // =====================================================

    public async Task<IActionResult> Dashboard()
    {
        var model = new
        {
            Restaurants = await _context.Restaurants.CountAsync(),
            Offers = await _context.Offers.CountAsync(),
            Reservations = await _context.Reservations.CountAsync(),
            Reviews = await _context.Reviews.CountAsync()
        };

        return View(model);
    }

    // =====================================================
    // RESTAURANTS
    // =====================================================

    public async Task<IActionResult> Restaurants(string? search, int page = 1)
    {
        var query = _context.Restaurants
            .Include(r => r.Owner)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r =>
                r.Name.Contains(search) ||
                r.BusinessEmail.Contains(search));
        }

        var total = await query.CountAsync();

        var restaurants = await query
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Search = search;

        return View(restaurants);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var restaurant = await _context.Restaurants
            .Include(r => r.Owner)
            .Include(r => r.Offers)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (restaurant == null)
            return NotFound();

        return View(restaurant);
    }

    // =====================================================
    // OFFERS
    // =====================================================

    public async Task<IActionResult> Offers(string? search, int page = 1)
    {
        var query = _context.Offers
            .Include(o => o.Restaurant)
            .Include(o => o.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o =>
                o.Title.Contains(search) ||
                o.Restaurant.Name.Contains(search));
        }

        var total = await query.CountAsync();

        var offers = await query
            .OrderByDescending(o => o.EndsAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Search = search;

        return View(offers);
    }

    public async Task<IActionResult> OfferDetails(Guid id)
    {
        var offer = await _context.Offers
            .Include(o => o.Restaurant)
            .Include(o => o.Category)                     
            .Include(o => o.OfferTags)                    
                .ThenInclude(ot => ot.Tag)                
            .FirstOrDefaultAsync(o => o.Id == id);

        if (offer == null)
            return NotFound();

        return View(offer);
    }

    // =====================================================
    // RESERVATIONS (ORDERS)
    // =====================================================

    public async Task<IActionResult> Reservations(string? search, int page = 1)
    {
        var query = _context.Reservations
            .Include(r => r.User)
            .Include(r => r.Items)
                .ThenInclude(i => i.Offer)
                    .ThenInclude(o => o.Restaurant)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r =>
                r.Id.ToString().Contains(search) ||
                r.User.Email.Contains(search) ||
                r.Items.Any(i => i.Offer.Restaurant.Name.Contains(search)));
        }

        var total = await query.CountAsync();

        var reservations = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Search = search;

        return View(reservations);
    }

    public async Task<IActionResult> ReservationDetails(Guid id)
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

        return View(reservation);
    }

    // =====================================================
    // REVIEWS
    // =====================================================

    public async Task<IActionResult> Reviews(string? search, int page = 1)
    {
        var query = _context.Reviews
            .Include(r => r.Reservation)
                .ThenInclude(res => res.User)
            .Include(r => r.Reservation)
                .ThenInclude(res => res.Items)
                    .ThenInclude(i => i.Offer)
                        .ThenInclude(o => o.Restaurant)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r =>
                (r.Comment != null && r.Comment.Contains(search)) ||
                r.Reservation.User.Email.Contains(search) ||
                r.Reservation.Items.Any(i =>
                    i.Offer.Restaurant.Name.Contains(search)));
        }

        var total = await query.CountAsync();

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Search = search;

        return View(reviews);
    }

    public async Task<IActionResult> ReviewDetails(Guid id)
    {
        var review = await _context.Reviews
            .Include(r => r.Reservation)
                .ThenInclude(res => res.User)          
            .Include(r => r.Reservation)
                .ThenInclude(res => res.Items)
                    .ThenInclude(i => i.Offer)
                        .ThenInclude(o => o.Restaurant)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
            return NotFound();

        return View(review);
    }
}