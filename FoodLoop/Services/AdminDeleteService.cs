using FoodLoop.Data;
using FoodLoop.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodLoop.Services
{
    public class AdminDeleteService
    {
        private readonly ApplicationDbContext _context;

        public AdminDeleteService(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // DELETE RESTAURANT
        // =====================================================
        public async Task DeleteRestaurantAsync(Guid restaurantId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == restaurantId);

            if (restaurant == null)
                return;

            // Всички Offer-и на ресторанта
            var offerIds = await _context.Offers
                .Where(o => o.RestaurantId == restaurantId)
                .Select(o => o.Id)
                .ToListAsync();

            if (offerIds.Any())
            {
                // ReservationItems за тези offers
                var reservationItems = await _context.Set<ReservationItem>()
                    .Where(i => offerIds.Contains(i.OfferId))
                    .ToListAsync();

                var reservationIds = reservationItems
                    .Select(i => i.ReservationId)
                    .Distinct()
                    .ToList();

                // Reviews (през Reservation)
                var reviews = await _context.Reviews
                    .Where(rv => reservationIds.Contains(rv.ReservationId))
                    .ToListAsync();

                _context.Reviews.RemoveRange(reviews);

                // Status logs
                var logs = await _context.Set<ReservationStatusLog>()
                    .Where(l => reservationIds.Contains(l.ReservationId))
                    .ToListAsync();

                _context.Set<ReservationStatusLog>()
                    .RemoveRange(logs);

                // 5️⃣ ReservationItems
                _context.Set<ReservationItem>()
                    .RemoveRange(reservationItems);

                // Reservations
                var reservations = await _context.Reservations
                    .Where(r => reservationIds.Contains(r.Id))
                    .ToListAsync();

                _context.Reservations.RemoveRange(reservations);

                // OfferTags
                var offerTags = await _context.OfferTags
                    .Where(x => offerIds.Contains(x.OfferId))
                    .ToListAsync();

                _context.OfferTags.RemoveRange(offerTags);

                // 8️⃣ Offers
                var offers = await _context.Offers
                    .Where(o => offerIds.Contains(o.Id))
                    .ToListAsync();

                _context.Offers.RemoveRange(offers);
            }

            // Restaurant

            // Remove the restaurant
            _context.Restaurants.Remove(restaurant);

            // Remove the user identity
            var ownerUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == restaurant.OwnerUserId);

            if (ownerUser != null)
            {
                _context.Users.Remove(ownerUser);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // =====================================================
        // DELETE OFFER
        // =====================================================
        public async Task DeleteOfferAsync(Guid offerId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var offer = await _context.Offers
                .FirstOrDefaultAsync(o => o.Id == offerId);

            if (offer == null)
                return;

            var reservationItems = await _context.Set<ReservationItem>()
                .Where(i => i.OfferId == offerId)
                .ToListAsync();

            var reservationIds = reservationItems
                .Select(i => i.ReservationId)
                .Distinct()
                .ToList();

            // Reviews
            var reviews = await _context.Reviews
                .Where(rv => reservationIds.Contains(rv.ReservationId))
                .ToListAsync();

            _context.Reviews.RemoveRange(reviews);

            // StatusLogs
            var logs = await _context.Set<ReservationStatusLog>()
                .Where(l => reservationIds.Contains(l.ReservationId))
                .ToListAsync();

            _context.Set<ReservationStatusLog>().RemoveRange(logs);

            // ReservationItems
            _context.Set<ReservationItem>().RemoveRange(reservationItems);

            // Reservations
            var reservations = await _context.Reservations
                .Where(r => reservationIds.Contains(r.Id))
                .ToListAsync();

            _context.Reservations.RemoveRange(reservations);

            // OfferTags
            var offerTags = await _context.OfferTags
                .Where(x => x.OfferId == offerId)
                .ToListAsync();

            _context.OfferTags.RemoveRange(offerTags);

            // Offer
            _context.Offers.Remove(offer);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // =====================================================
        // DELETE RESERVATION
        // =====================================================
        public async Task DeleteReservationAsync(Guid reservationId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReservationId == reservationId);

            if (review != null)
                _context.Reviews.Remove(review);

            var logs = await _context.Set<ReservationStatusLog>()
                .Where(l => l.ReservationId == reservationId)
                .ToListAsync();

            _context.Set<ReservationStatusLog>().RemoveRange(logs);

            var items = await _context.Set<ReservationItem>()
                .Where(i => i.ReservationId == reservationId)
                .ToListAsync();

            _context.Set<ReservationItem>().RemoveRange(items);

            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation != null)
                _context.Reservations.Remove(reservation);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        // =====================================================
        // DELETE REVIEW
        // =====================================================
        public async Task DeleteReviewAsync(Guid reviewId)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }
        }
    }
}