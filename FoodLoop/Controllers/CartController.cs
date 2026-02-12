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

        // --------------------------------------------------------------
        // CART PAGE (Checkout section + Ongoing orders + History)
        // --------------------------------------------------------------
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Cart items for "Order checkout"
            var cartItems = await _context.CartItems
                .Where(c => c.UserId == user.Id)
                .Include(c => c.Offer)
                .ThenInclude(o => o.Restaurant)
                .ToListAsync();

            // Reservations (ongoing + history)
            var reservations = await _context.Reservations
                .Where(r => r.UserId == user.Id)
                .Include(r => r.Offer)
                .ThenInclude(o => o.Restaurant)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var model = new CartPageViewModel
            {
                CartItems = cartItems,
                Reservations = reservations
            };

            return View(model);
        }

        // --------------------------------------------------------------
        // ADD TO CART
        // --------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> AddToCart(Guid offerId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var offer = await _context.Offers.FindAsync(offerId);
            if (offer == null)
            {
                TempData["Error"] = "Offer not found.";
                return RedirectToAction("Index", "Home");
            }

            bool exists = await _context.CartItems
                .AnyAsync(c => c.UserId == user.Id && c.OfferId == offerId);

            if (exists)
            {
                TempData["Error"] = "This offer is already in your cart.";
                return RedirectToAction("Index");
            }

            _context.CartItems.Add(new CartItem
            {
                UserId = user.Id,
                OfferId = offerId,
                Quantity = 1,
                AddedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Offer added to cart!";
            return RedirectToAction("Index");
        }

        // --------------------------------------------------------------
        // REMOVE FROM CART
        // --------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);

            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);

            if (item == null)
            {
                TempData["Error"] = "Item not found.";
                return RedirectToAction("Index");
            }

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Item removed.";
            return RedirectToAction("Index");
        }

        // --------------------------------------------------------------
        // INCREASE QUANTITY
        // --------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> IncreaseQuantity(Guid itemId)
        {
            var item = await _context.CartItems
                .Include(c => c.Offer)
                .FirstOrDefaultAsync(c => c.Id == itemId);

            if (item == null)
                return RedirectToAction("Index");

            if (item.Quantity < item.Offer.QuantityAvailable)
            {
                item.Quantity++;
                await _context.SaveChangesAsync();
            }
            else
            {
                TempData["Error"] = "Not enough items available.";
            }

            return RedirectToAction("Index");
        }

        // --------------------------------------------------------------
        // DECREASE QUANTITY
        // --------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> DecreaseQuantity(Guid itemId)
        {
            var item = await _context.CartItems.FindAsync(itemId);

            if (item == null)
                return RedirectToAction("Index");

            if (item.Quantity > 1)
            {
                item.Quantity--;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // --------------------------------------------------------------
        // CHECKOUT → Converts CartItems into Reservations
        // --------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Checkout()
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
                return RedirectToAction("Index");
            }

            foreach (var item in cartItems)
            {
                // 1) Validate stock
                if (item.Offer.QuantityAvailable < item.Quantity)
                {
                    TempData["Error"] = $"Not enough stock for {item.Offer.Title}.";
                    return RedirectToAction("Index");
                }

                // 2) Deduct stock
                item.Offer.QuantityAvailable -= item.Quantity;

                // 3) Create reservation
                _context.Reservations.Add(new Reservation
                {
                    UserId = user.Id,
                    OfferId = item.OfferId,
                    Quantity = item.Quantity,
                    Status = ReservationStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // 4) Clear cart
            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Your order has been placed!";
            return RedirectToAction("Index");
        }
    }
}