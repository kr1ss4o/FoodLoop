using FoodLoop.Data;
using FoodLoop.Models.Entities;
using FoodLoop.Models.Enums;
using FoodLoop.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class ReservationService : IReservationService
{
    private readonly ApplicationDbContext _context;

    public ReservationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task ExpirePendingReservationsAsync()
    {
        var now = DateTime.UtcNow;

        var expiredReservations = await _context.Reservations
            .Include(r => r.StatusLogs)
            .Where(r =>
                r.Status == ReservationStatus.Pending &&
                EF.Functions.DateDiffMinute(r.CreatedAt, now) > 30)
            .ToListAsync();

        if (!expiredReservations.Any())
            return;

        foreach (var r in expiredReservations)
        {
            r.Status = ReservationStatus.Canceled;

            r.StatusLogs.Add(new ReservationStatusLog
            {
                Id = Guid.NewGuid(),
                ReservationId = r.Id,
                NewStatus = ReservationStatus.Canceled,
                ChangedAt = now
            });

            foreach (var item in r.Items)
            {
                item.Offer.QuantityAvailable += item.Quantity;
            }
        }

        await _context.SaveChangesAsync();
    }
}