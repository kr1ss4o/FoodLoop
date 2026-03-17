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
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;

        public ReviewsController(ApplicationDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // =====================================================
        // CREATE (GET)
        // =====================================================

        public async Task<IActionResult> Create(Guid reservationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var reservation = await _db.Reservations
                .Include(r => r.StatusLogs)
                .Include(r => r.Review)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null) return NotFound();

            if (reservation.UserId != user.Id) return Forbid();

            if (reservation.Status != ReservationStatus.Finished)
                return BadRequest("Поръчката не е завършена.");

            if (reservation.Review != null)
                return BadRequest("Вече има изпратено ревю.");

            var finishedLog = reservation.StatusLogs
                .Where(l => l.NewStatus == ReservationStatus.Finished)
                .OrderByDescending(l => l.ChangedAt)
                .FirstOrDefault();

            if (finishedLog == null ||
                finishedLog.ChangedAt.AddDays(3) < DateTime.UtcNow)
                return BadRequest("Периода за ревю изтече.");

            var model = new ReviewFormViewModel
            {
                ReservationId = reservation.Id,
                Rating = 5
            };

            ViewData["Title"] = "Остави ревю";

            return View("ReviewForm", model);
        }

        // =====================================================
        // CREATE (POST)
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReviewFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View("ReviewForm", model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var reservation = await _db.Reservations
                .Include(r => r.StatusLogs)
                .Include(r => r.Review)
                .FirstOrDefaultAsync(r => r.Id == model.ReservationId);

            if (reservation == null) return NotFound();

            if (reservation.UserId != user.Id) return Forbid();

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
                return BadRequest("Периода за ревю изтече.");

            if (model.Rating < 1 || model.Rating > 5)
            {
                ModelState.AddModelError("", "Невалиден рейтинг.");
                return View("ReviewForm", model);
            }

            var review = new Review
            {
                ReservationId = reservation.Id,
                Rating = model.Rating,
                Comment = string.IsNullOrWhiteSpace(model.Comment)
                    ? null
                    : model.Comment.Trim()
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Cart", new { tab = "history" });
        }

        // =====================================================
        // EDIT (GET)
        // =====================================================

        public async Task<IActionResult> Edit(Guid reservationId)
        {
            ModelState.Clear();

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

            var model = new ReviewFormViewModel
            {
                ReservationId = reservation.Id,
                Rating = reservation.Review.Rating,
                Comment = reservation.Review.Comment
            };

            ViewData["Title"] = "Редактирай ревю";

            return View("ReviewForm", model);
        }

        // =====================================================
        // EDIT (POST)
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ReviewFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View("ReviewForm", model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var reservation = await _db.Reservations
                .Include(r => r.StatusLogs)
                .Include(r => r.Review)
                .FirstOrDefaultAsync(r => r.Id == model.ReservationId);

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

            if (model.Rating < 1 || model.Rating > 5)
            {
                ModelState.AddModelError("", "Невалиден рейтинг.");
                return View("ReviewForm", model);
            }

            reservation.Review.Rating = model.Rating;
            reservation.Review.Comment = string.IsNullOrWhiteSpace(model.Comment)
                ? null
                : model.Comment.Trim();

            await _db.SaveChangesAsync();

            return RedirectToAction("Index", "Cart", new { tab = "history" });
        }

        // =====================================================
        // DELETE
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
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