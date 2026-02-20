using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.Enums;
using FoodLoop.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public CartController(ApplicationDbContext context,
                              UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==============================================================
        // CART PAGE 
        // ==============================================================

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == user.Id)
                .Include(c => c.Offer)
                    .ThenInclude(o => o.Restaurant)
                .ToListAsync();

            var reservations = await _context.Reservations
                .Where(r => r.UserId == user.Id)
                .Include(r => r.Items)
                    .ThenInclude(i => i.Offer)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var model = new CartPageViewModel
            {
                CartItems = cartItems,
                Reservations = reservations
            };

            return View(model);
        }


        // ==============================================================
        // MINI CART
        // ==============================================================

        [HttpGet]
        public async Task<IActionResult> MiniCart()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Content("");

            var items = await _context.CartItems
                .Where(c => c.UserId == user.Id)
                .Include(c => c.Offer)
                .Take(3)
                .ToListAsync();

            return PartialView("_MiniCartPartial", items);
        }

        // ==============================================================
        // ADD TO CART
        // ==============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(Guid offerId, int quantity = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false });

            var offer = await _context.Offers
                .FirstOrDefaultAsync(o => o.Id == offerId);

            if (offer == null ||
                offer.EndsAt <= DateTime.UtcNow ||
                offer.QuantityAvailable < quantity)
            {
                return Json(new { success = false });
            }

            var existing = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.OfferId == offerId);

            if (existing != null)
                existing.Quantity += quantity;
            else
                _context.CartItems.Add(new CartItem
                {
                    UserId = user.Id,
                    OfferId = offerId,
                    Quantity = quantity
                });

            await _context.SaveChangesAsync();

            var cartCount = await _context.CartItems
                .Where(c => c.UserId == user.Id)
                .SumAsync(c => c.Quantity);

            return Json(new
            {
                success = true,
                cartCount
            });
        }
        // ==============================================================
        // INCREASE QUANTITY
        // ==============================================================

        [HttpPost]
        public async Task<IActionResult> Increase(Guid itemId)
        {
            var user = await _userManager.GetUserAsync(User);

            var item = await _context.CartItems
                .Include(c => c.Offer)
                .FirstOrDefaultAsync(c => c.Id == itemId && c.UserId == user.Id);

            if (item == null)
                return RedirectToAction("Index");

            if (item.Quantity < item.Offer.QuantityAvailable)
            {
                item.Quantity++;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }


        // ==============================================================
        // DECREASE QUANTITY
        // ==============================================================

        [HttpPost]
        public async Task<IActionResult> Decrease(Guid itemId)
        {
            var user = await _userManager.GetUserAsync(User);

            var item = await _context.CartItems
                .Include(c => c.Offer)
                .FirstOrDefaultAsync(c => c.Id == itemId && c.UserId == user.Id);

            if (item == null)
                return RedirectToAction("Index");

            if (item.Quantity > 1)
            {
                item.Quantity--;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }


        // ==============================================================
        // REMOVE
        // ==============================================================

        [HttpPost]
        public async Task<IActionResult> Remove(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);

            if (item == null)
                return RedirectToAction("Index");

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // ==============================================================
        // CHECKOUT
        // ==============================================================

        [HttpPost]
        public async Task<IActionResult> Checkout(
            string deliveryType,
            bool isForSomeoneElse,
            string? recipientFullName,
            string? recipientPhone)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == user.Id)
                .Include(c => c.Offer)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Index");

            // If the order is for someone else, recipient fields are required.
            // If not, ignore anything posted in recipientFullName/recipientPhone.
            if (isForSomeoneElse)
            {
                if (string.IsNullOrWhiteSpace(recipientFullName) || string.IsNullOrWhiteSpace(recipientPhone))
                {
                    TempData["Error"] = "Recipient name and phone are required when ordering for someone else.";
                    return RedirectToAction("Index");
                }
            }

            var reservation = new Reservation
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                Status = ReservationStatus.Pending,
                DeliveryType = deliveryType,
                IsForSomeoneElse = isForSomeoneElse,
                RecipientFullName = isForSomeoneElse ? recipientFullName?.Trim() : null,
                RecipientPhone = isForSomeoneElse ? recipientPhone?.Trim() : null,
                TotalPrice = 0
            };

            foreach (var item in cartItems)
            {
                item.Offer.QuantityAvailable -= item.Quantity;

                reservation.Items.Add(new ReservationItem
                {
                    OfferId = item.OfferId,
                    Quantity = item.Quantity,
                    PriceSnapshot = item.Offer.DiscountedPrice
                });

                reservation.TotalPrice +=
                    item.Quantity * item.Offer.DiscountedPrice;
            }

            _context.Reservations.Add(reservation);
            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}