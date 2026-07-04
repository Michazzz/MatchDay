using MassTransit;
using MatchDayContracts;
using ReservationService.Data;
using ReservationService.Domain;

namespace ReservationService.Consumers;

/// <summary>MatchService held the seats → mark the reservation Reserved and notify.</summary>
public class SeatsReservedConsumer(ReservationDbContext db) : IConsumer<SeatsReserved>
{
    public async Task Consume(ConsumeContext<SeatsReserved> context)
    {
        var msg = context.Message;
        var reservation = await db.Reservations.FindAsync([msg.ReservationId], context.CancellationToken);
        if (reservation is null || reservation.Status == ReservationStatus.Reserved)
        {
            return; // unknown or already applied — idempotent no-op
        }

        reservation.Status = ReservationStatus.Reserved;
        reservation.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(context.CancellationToken);

        await context.Publish(new ReservationConfirmed
        {
            ReservationId = reservation.Id,
            MatchId = reservation.MatchId,
            SectorId = reservation.SectorId,
            Quantity = reservation.Quantity,
            CustomerEmail = reservation.CustomerEmail
        });
    }
}
