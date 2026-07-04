using MassTransit;
using MatchDayContracts;
using MatchService.Data;
using MatchService.Domain;
using Microsoft.EntityFrameworkCore;

namespace MatchService.Consumers;

/// <summary>
/// Holds seats for a reservation. MatchService owns availability, so this is where
/// oversell and duplicate deliveries are prevented:
///  - a pessimistic row lock (SELECT … FOR UPDATE) serializes concurrent reservations
///    for the same sector (race condition on the last seats);
///  - a SeatHold keyed by ReservationId makes a redelivered command a no-op (idempotency).
/// </summary>
public class ReserveSeatsConsumer(MatchDbContext db) : IConsumer<ReserveSeats>
{
    public async Task Consume(ConsumeContext<ReserveSeats> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Idempotency: already processed (e.g. a redelivery) → re-affirm success and stop.
        var existingHold = await db.SeatHolds.FirstOrDefaultAsync(s => s.ReservationId == msg.ReservationId, ct);
        if (existingHold is not null)
        {
            await tx.CommitAsync(ct);
            await PublishReserved(context, msg, existingHold.TotalPrice);
            return;
        }

        // Lock the sector row for the duration of the transaction.
        var sector = (await db.Sectors
            .FromSql($"SELECT * FROM \"Sectors\" WHERE \"Id\" = {msg.SectorId} FOR UPDATE")
            .ToListAsync(ct)).FirstOrDefault();

        if (sector is null || sector.MatchId != msg.MatchId)
        {
            await tx.RollbackAsync(ct);
            await PublishRejected(context, msg, "Match or sector not found.");
            return;
        }

        if (msg.Quantity <= 0)
        {
            await tx.RollbackAsync(ct);
            await PublishRejected(context, msg, "Quantity must be greater than zero.");
            return;
        }

        if (sector.AvailableSeats < msg.Quantity)
        {
            await tx.RollbackAsync(ct);
            await PublishRejected(context, msg, "Not enough seats available.");
            return;
        }

        sector.AvailableSeats -= msg.Quantity;
        var totalPrice = sector.Price * msg.Quantity;

        db.SeatHolds.Add(new SeatHold
        {
            ReservationId = msg.ReservationId,
            SectorId = msg.SectorId,
            Quantity = msg.Quantity,
            HeldAt = DateTimeOffset.UtcNow,
            TotalPrice = totalPrice
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await PublishReserved(context, msg, totalPrice);
    }

    private static Task PublishReserved(ConsumeContext context, ReserveSeats msg, decimal totalPrice) =>
        context.Publish(new SeatsReserved
        {
            ReservationId = msg.ReservationId,
            MatchId = msg.MatchId,
            SectorId = msg.SectorId,
            Quantity = msg.Quantity,
            TotalPrice = totalPrice,
        });

    private static Task PublishRejected(ConsumeContext context, ReserveSeats msg, string reason) =>
        context.Publish(new SeatsReservationRejected
        {
            ReservationId = msg.ReservationId,
            Reason = reason
        });
}
