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
    [Authorize(Roles = "Client,Admin")]
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
                .Include(r => r.StatusLogs)
                .Include(r => r.Review)
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
                return Json(new { success = false, message = "Not logged in" });

            // ❗ ROLE CHECK
            if (!User.IsInRole("Client"))
                return Json(new { success = false, message = "Not allowed" });

            var offer = await _context.Offers
                .Include(o => o.Restaurant) // 🔥 ВАЖНО
                .FirstOrDefaultAsync(o => o.Id == offerId);

            if (offer == null ||
                offer.EndsAt <= DateTime.UtcNow ||
                offer.QuantityAvailable < quantity)
            {
                return Json(new { success = false, message = "Invalid offer" });
            }

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == user.Id)
                .Include(c => c.Offer)
                .ToListAsync();

            // 🔴 MAIN LOGIC – само 1 ресторант
            if (cartItems.Any())
            {
                var existingRestaurantId = cartItems.First().Offer.RestaurantId;

                if (existingRestaurantId != offer.RestaurantId)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Можеш да поръчваш само от един ресторант."
                    });
                }
            }

            var existing = cartItems.FirstOrDefault(c => c.OfferId == offerId);

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
                return Json(new { success = false });

            if (item.Quantity < item.Offer.QuantityAvailable)
            {
                item.Quantity++;
                await _context.SaveChangesAsync();
            }

            var newTotal = await _context.CartItems
            .Where(c => c.UserId == user.Id)
            .Select(c => c.Offer.DiscountedPrice * c.Quantity)
            .SumAsync();

            return Json(new
            {
                success = true,
                quantity = item.Quantity,
                itemTotal = item.Quantity * item.Offer.DiscountedPrice,
                cartTotal = newTotal
            });
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
                return Json(new { success = false });

            if (item.Quantity > 1)
            {
                item.Quantity--;
                await _context.SaveChangesAsync();
            }

            var newTotal = await _context.CartItems
                .Where(c => c.UserId == user.Id)
                .SumAsync(c => c.Quantity * c.Offer.DiscountedPrice);

            return Json(new
            {
                success = true,
                quantity = item.Quantity,
                itemTotal = item.Quantity * item.Offer.DiscountedPrice,
                cartTotal = newTotal
            });
        }

        // ==============================================================
        // REMOVE
        // ==============================================================

        [HttpPost]
        public async Task<IActionResult> Remove(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);

            var item = await _context.CartItems
                .Include(c => c.Offer)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.Id);

            if (item == null)
                return Json(new { success = false });

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            var newTotal = await _context.CartItems
                .Where(c => c.UserId == user.Id)
                .SumAsync(c => c.Quantity * c.Offer.DiscountedPrice);

            return Json(new
            {
                success = true,
                cartTotal = newTotal
            });
        }

        // ==============================================================
        // CHECKOUT
        // ==============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(string deliveryType, bool IsForSomeoneElse, string? RecipientFullName,   string? RecipientPhone, string? DeliveryAddress)
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

            // VALIDATE recipient if needed
            if (IsForSomeoneElse)
            {
                if (string.IsNullOrWhiteSpace(RecipientFullName))
                {
                    TempData["Error"] = "Изисква се име на получател.";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrWhiteSpace(RecipientPhone) ||
                    RecipientPhone.Length != 10 ||
                    !RecipientPhone.All(char.IsDigit))
                {
                    TempData["Error"] = "Телефонът трябва да е дълъг 10 символа.";
                    return RedirectToAction("Index");
                }
            }

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var reservation = new Reservation
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    Status = ReservationStatus.Pending,
                    DeliveryType = deliveryType,
                    DeliveryAddress = deliveryType == "Delivery" ? DeliveryAddress : null,
                    TotalPrice = 0,

                    IsForSomeoneElse = IsForSomeoneElse,
                    RecipientFullName = IsForSomeoneElse ? RecipientFullName : null,
                    RecipientPhone = IsForSomeoneElse ? RecipientPhone : null
                };

                foreach (var item in cartItems)
                {
                    var offer = await _context.Offers
                        .Where(o => o.Id == item.OfferId)
                        .FirstOrDefaultAsync();

                    if (offer == null)
                    {
                        TempData["Error"] = "Офертата не бе намерена.";
                        return RedirectToAction("Index");
                    }

                    if (offer.QuantityAvailable < item.Quantity)
                    {
                        TempData["Error"] = $"Недостатъчна наличност на {offer.Title}.";
                        return RedirectToAction("Index");
                    }

                    offer.QuantityAvailable -= item.Quantity;

                    reservation.Items.Add(new ReservationItem
                    {
                        OfferId = offer.Id,
                        Quantity = item.Quantity,
                        PriceSnapshot = offer.DiscountedPrice
                    });

                    reservation.TotalPrice += item.Quantity * offer.DiscountedPrice;
                }
                // Adds additional delivery fee of 2 Euro
                if (deliveryType == "Delivery")
                {
                    reservation.TotalPrice += 2m;
                }

                _context.Reservations.Add(reservation);
                _context.CartItems.RemoveRange(cartItems);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Success"] = "Поръчката бе направена успешно.";
                return RedirectToAction("Index");
            }
            catch
            {
                await tx.RollbackAsync();
                TempData["Error"] = "Нещо се обърка.";
                return RedirectToAction("Index");
            }
        }
    }
}