using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.Enums;
using FoodLoop.Models.ViewModels;
using FoodLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace FoodLoop.Controllers
{
    [Authorize(Roles = "Restaurant")]
    public class RestaurantDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IDashboardAnalyticsService _analyticsService;

        public RestaurantDashboardController(ApplicationDbContext context, UserManager<User> userManager, IDashboardAnalyticsService analyticsService)
        {
            _context = context;
            _userManager = userManager;
            _analyticsService = analyticsService;
        }

        public async Task<IActionResult> Index(DateTime? date)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var vm = await _analyticsService.BuildDashboardAsync(user.Id, date);

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(DateTime? from, DateTime? to)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var restaurant = await _context.Restaurants
                .Where(r => r.OwnerUserId == user.Id)
                .Select(r => new { r.Id, r.Name })
                .FirstOrDefaultAsync();

            if (restaurant == null) return Forbid();

            // Bulgarian time
            var tz = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;

            var fromLocal = (from?.Date ?? nowLocal.AddDays(-29));
            var toLocalExclusive = (to?.Date ?? nowLocal).AddDays(1); // inclusive day -> exclusive end

            var fromUtc = TimeZoneInfo.ConvertTimeToUtc(fromLocal, tz);
            var toUtc = TimeZoneInfo.ConvertTimeToUtc(toLocalExclusive, tz);

            // NOTE: CreatedAt е UTC в БД, затова филтрирането е по UTC
            var rows = await _context.Reservations
                .AsNoTracking()
                .Where(r =>
                    r.CreatedAt >= fromUtc && r.CreatedAt < toUtc &&
                    r.Items.Any(i => i.Offer.RestaurantId == restaurant.Id))
                .Include(r => r.User)
                .Include(r => r.Items).ThenInclude(i => i.Offer)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.CreatedAt,
                    r.Status,
                    r.DeliveryType,
                    r.TotalPrice,
                    CustomerName = r.IsForSomeoneElse ? r.RecipientFullName : r.User.FullName,
                    Phone = r.IsForSomeoneElse ? r.RecipientPhone : r.User.PhoneNumber,
                    Items = r.Items.Select(i => new { i.Offer.Title, i.Quantity }).ToList()
                })
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("OrderId,CreatedAtLocal,Status,DeliveryType,CustomerName,Phone,TotalPrice,Items");

            foreach (var r in rows)
            {
                var createdLocal = TimeZoneInfo.ConvertTimeFromUtc(r.CreatedAt, tz);

                // items: "Offer A x2 | Offer B x1"
                var itemsText = string.Join(" | ", r.Items.Select(x => $"{x.Title} x{x.Quantity}"));

                sb.AppendLine(string.Join(",",
                    Csv(r.Id.ToString()),
                    Csv(createdLocal.ToString("yyyy-MM-dd HH:mm")),
                    Csv(r.Status.ToString()),
                    Csv(r.DeliveryType ?? ""),
                    Csv(r.CustomerName ?? ""),
                    Csv(r.Phone ?? ""),
                    Csv(r.TotalPrice.ToString("0.00")),
                    Csv(itemsText)
                ));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"orders_{fromLocal:yyyy-MM-dd}_to_{toLocalExclusive.AddDays(-1):yyyy-MM-dd}.csv";

            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        private static string Csv(string value)
        {
            value ??= "";
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }
    }
}