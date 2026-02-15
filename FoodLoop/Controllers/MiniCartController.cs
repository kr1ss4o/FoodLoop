using FoodLoop.Data;
using FoodLoop.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FoodLoop.Controllers
{
    [Authorize(Roles = "Client")]
    public class MiniCartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public MiniCartController(ApplicationDbContext context,
                                  UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetMiniCart()
        {
            var user = await _userManager.GetUserAsync(User);

            var items = _context.CartItems
                .Where(c => c.UserId == user.Id)
                .Select(c => new
                {
                    c.Id,
                    c.Quantity,
                    Title = c.Offer.Title,
                    Price = c.Offer.DiscountedPrice
                })
                .ToList();

            return Json(items);
        }
    }

}
