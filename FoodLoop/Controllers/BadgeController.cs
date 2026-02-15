using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class BadgeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public BadgeController(ApplicationDbContext context,
                           UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetCounts()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new { cart = 0, pending = 0 });

        var roles = await _userManager.GetRolesAsync(user);

        int cartCount = 0;
        int pendingCount = 0;

        // ================= CLIENT BADGE =================
        if (roles.Contains("Client"))
        {
            cartCount = await _context.CartItems
                .CountAsync(c => c.UserId == user.Id);
        }

        // ================= RESTAURANT BADGE =================
        if (roles.Contains("Restaurant"))
        {
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.OwnerUserId == user.Id);

            if (restaurant != null)
            {
                pendingCount = await _context.Reservations
                    .Include(r => r.Items)
                        .ThenInclude(i => i.Offer)
                    .Where(r =>
                        r.Status == ReservationStatus.Pending &&
                        r.Items.Any(i => i.Offer.RestaurantId == restaurant.Id))
                    .CountAsync();
            }
        }

        return Json(new
        {
            cart = cartCount,
            pending = pendingCount
        });
    }
}