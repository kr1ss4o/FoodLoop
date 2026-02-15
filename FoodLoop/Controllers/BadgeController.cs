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
    [HttpGet]
    public async Task<IActionResult> GetCounts()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Json(new { cart = 0, pending = 0 });

        var roles = await _userManager.GetRolesAsync(user);

        int cartCount = 0;
        int pendingCount = 0;

        if (roles.Contains("Client"))
        {
            cartCount = _context.CartItems
                .Count(c => c.UserId == user.Id);
        }

        if (roles.Contains("Restaurant"))
        {
            var restaurantId = _context.Restaurants
                .Where(r => r.OwnerUserId == user.Id)
                .Select(r => (Guid?)r.Id)
                .FirstOrDefault();

            if (restaurantId != null)
            {
                pendingCount = _context.Reservations
                    .Count(r =>
                        r.Status == ReservationStatus.Pending &&
                        r.Items.Any(i =>
                            i.Offer.RestaurantId == restaurantId.Value));
            }
        }

        return Json(new
        {
            cart = cartCount,
            pending = pendingCount
        });
    }
}