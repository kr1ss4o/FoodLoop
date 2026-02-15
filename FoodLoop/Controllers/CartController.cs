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

        public CartController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ==============================================================
        // ADD TO CART
        // ==============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(Guid offerId, int quantity)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false });

            var offer = await _context.Offers
                .FirstOrDefaultAsync(o => o.Id == offerId);

            if (offer == null)
                return Json(new { success = false });

            if (quantity < 1) quantity = 1;
            if (quantity > offer.QuantityAvailable)
                quantity = offer.QuantityAvailable;

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.OfferId == offerId);

            if (existingItem != null)
                existingItem.Quantity += quantity;
            else
                _context.CartItems.Add(new CartItem
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    OfferId = offerId,
                    Quantity = quantity
                });

            await _context.SaveChangesAsync();

            var cartCount = await _context.CartItems
                .Where(c => c.UserId == user.Id)
                .SumAsync(c => c.Quantity);

            return Json(new { success = true, cartCount });
        }

        // ==============================================================
        // REMOVE FROM CART
        // ==============================================================

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);

            if (item == null)
            {
                TempData["Error"] = "Item not found.";
                return RedirectToAction("Index", "Orders");
            }

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Item removed.";
            return RedirectToAction("Index", "Orders");
        }

        // ==============================================================
        // INCREASE QUANTITY
        // ==============================================================

        [HttpPost]
        public async Task<IActionResult> IncreaseQuantity(Guid itemId)
        {
            var item = await _context.CartItems
                .Include(c => c.Offer)
                .FirstOrDefaultAsync(c => c.Id == itemId);

            if (item == null)
                return RedirectToAction("Index", "Orders");

            if (item.Quantity < item.Offer.QuantityAvailable)
            {
                item.Quantity++;
                await _context.SaveChangesAsync();
            }
            else
            {
                TempData["Error"] = "Not enough items available.";
            }

            return RedirectToAction("Index", "Orders");
        }

        // ==============================================================
        // DECREASE QUANTITY
        // ==============================================================

        [HttpPost]
        public async Task<IActionResult> DecreaseQuantity(Guid itemId)
        {
            var item = await _context.CartItems.FindAsync(itemId);

            if (item == null)
                return RedirectToAction("Index", "Orders");

            if (item.Quantity > 1)
            {
                item.Quantity--;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Orders");
        }

        // ==============================================================
        // CHECKOUT
        // ==============================================================

        [HttpPost]
        public async Task<IActionResult> Checkout(string deliveryType, bool IsForSomeoneElse, string? RecipientFullName, string? RecipientPhone)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == user.Id)
                .Include(c => c.Offer)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index", "Orders");
            }

            // ==============================
            // VALIDATION FOR THIRD PERSON
            // ==============================
            if (IsForSomeoneElse)
            {
                if (string.IsNullOrWhiteSpace(RecipientFullName) ||
                    string.IsNullOrWhiteSpace(RecipientPhone))
                {
                    TempData["Error"] = "Recipient name and phone are required.";
                    return RedirectToAction("Index", "Orders");
                }

                if (RecipientPhone.Length != 10 || !RecipientPhone.All(char.IsDigit))
                {
                    TempData["Error"] = "Recipient phone must be exactly 10 digits.";
                    return RedirectToAction("Index", "Orders");
                }
            }

            var reservation = new Reservation
            {
                UserId = user.Id,
                Status = ReservationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                DeliveryType = deliveryType,
                TotalPrice = 0,

                IsForSomeoneElse = IsForSomeoneElse,
                RecipientFullName = IsForSomeoneElse ? RecipientFullName : null,
                RecipientPhone = IsForSomeoneElse ? RecipientPhone : null
            };

            foreach (var item in cartItems)
            {
                if (item.Offer.QuantityAvailable < item.Quantity)
                {
                    TempData["Error"] = $"Not enough stock for {item.Offer.Title}.";
                    return RedirectToAction("Index", "Orders");
                }

                item.Offer.QuantityAvailable -= item.Quantity;

                var reservationItem = new ReservationItem
                {
                    OfferId = item.OfferId,
                    Quantity = item.Quantity,
                    PriceSnapshot = item.Offer.DiscountedPrice
                };

                reservation.TotalPrice += item.Offer.DiscountedPrice * item.Quantity;

                reservation.Items.Add(reservationItem);
            }

            _context.Reservations.Add(reservation);
            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Your order has been placed!";
            return RedirectToAction("Index", "Orders");
        }

    }
}