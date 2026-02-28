using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Controllers
{
    [Authorize(Roles = "Restaurant,Admin")]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;

        public ReviewsController(ApplicationDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: /Reviews/Create/{reservationId}
        public async Task<IActionResult> Create(Guid reservationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var reservation = await _db.Reservations
                .Include(r => r.StatusLogs)
                .Include(r => r.Review)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
                return NotFound();

            // 1️⃣ Проверка: собственик
            if (reservation.UserId != user.Id)
                return Forbid();

            // 2️⃣ Проверка: Finished
            if (reservation.Status != ReservationStatus.Finished)
                return BadRequest("Reservation is not finished.");

            // 3️⃣ Проверка: вече има review
            if (reservation.Review != null)
                return BadRequest("Review already submitted.");

            // 4️⃣ Проверка: 3 дни срок
            var finishedLog = reservation.StatusLogs
                .Where(l => l.NewStatus == ReservationStatus.Finished)
                .OrderByDescending(l => l.ChangedAt)
                .FirstOrDefault();

            if (finishedLog == null)
                return BadRequest("Invalid reservation state.");

            if (finishedLog.ChangedAt.AddDays(3) < DateTime.UtcNow)
                return BadRequest("Review period expired.");

            ViewBag.ReservationId = reservation.Id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guid reservationId, int rating, string? comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var reservation = await _db.Reservations
                .Include(r => r.StatusLogs)
                .Include(r => r.Review)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
                return NotFound();

            if (reservation.UserId != user.Id)
                return Forbid();

            if (reservation.Status != ReservationStatus.Finished)
                return BadRequest();

            if (reservation.Review != null)
                return BadRequest();

            var finishedLog = reservation.StatusLogs
                .Where(l => l.NewStatus == ReservationStatus.Finished)
                .OrderByDescending(l => l.ChangedAt)
                .FirstOrDefault();

            if (finishedLog == null ||
                finishedLog.ChangedAt.AddDays(3) < DateTime.UtcNow)
                return BadRequest("Review period expired.");

            if (rating < 1 || rating > 5)
                return BadRequest("Invalid rating.");

            var review = new Review
            {
                ReservationId = reservation.Id,
                Rating = rating,
                Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Cart", new { tab = "history" });
        }
        //
        // Edit GET
        //
        public async Task<IActionResult> Edit(Guid reservationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var reservation = await _db.Reservations
                .Include(r => r.StatusLogs)
                .Include(r => r.Review)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null || reservation.Review == null)
                return NotFound();

            if (reservation.UserId != user.Id)
                return Forbid();

            var finishedLog = reservation.StatusLogs
                .Where(l => l.NewStatus == ReservationStatus.Finished)
                .OrderByDescending(l => l.ChangedAt)
                .FirstOrDefault();

            if (finishedLog == null ||
                finishedLog.ChangedAt.AddDays(3) < DateTime.UtcNow)
                return BadRequest();

            ViewBag.ReservationId = reservation.Id;
            ViewBag.Rating = reservation.Review.Rating;
            ViewBag.Comment = reservation.Review.Comment;

            return View();
        }
        //
        // Edit POST
        //
        [HttpPost]
        public async Task<IActionResult> Edit(Guid reservationId, int rating, string? comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var reservation = await _db.Reservations
                .Include(r => r.StatusLogs)
                .Include(r => r.Review)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null || reservation.Review == null)
                return NotFound();

            if (reservation.UserId != user.Id)
                return Forbid();

            var finishedLog = reservation.StatusLogs
                .Where(l => l.NewStatus == ReservationStatus.Finished)
                .OrderByDescending(l => l.ChangedAt)
                .FirstOrDefault();

            if (finishedLog == null ||
                finishedLog.ChangedAt.AddDays(3) < DateTime.UtcNow)
                return BadRequest();

            reservation.Review.Rating = rating;
            reservation.Review.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Cart", new { tab = "history" });
        }
        //
        // Delete
        //
        [HttpPost]
        public async Task<IActionResult> Delete(Guid reservationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var reservation = await _db.Reservations
                .Include(r => r.StatusLogs)
                .Include(r => r.Review)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation?.Review == null)
                return NotFound();

            if (reservation.UserId != user.Id)
                return Forbid();

            var finishedLog = reservation.StatusLogs
                .Where(l => l.NewStatus == ReservationStatus.Finished)
                .OrderByDescending(l => l.ChangedAt)
                .FirstOrDefault();

            if (finishedLog == null ||
                finishedLog.ChangedAt.AddDays(3) < DateTime.UtcNow)
                return BadRequest();

            _db.Reviews.Remove(reservation.Review);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Cart", new { tab = "history" });
        }
    }
}