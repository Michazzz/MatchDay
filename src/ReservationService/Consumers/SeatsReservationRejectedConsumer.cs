using MassTransit;
using MatchDayContracts;
using ReservationService.Data;
using ReservationService.Domain;

namespace ReservationService.Consumers;

/// <summary>MatchService could not hold the seats → mark the reservation Rejected and notify.</summary>
public class SeatsReservationRejectedConsumer(ReservationDbContext db) : IConsumer<SeatsReservationRejected>
{
    public async Task Consume(ConsumeContext<SeatsReservationRejected> context)
    {
        var msg = context.Message;
        var reservation = await db.Reservations.FindAsync([msg.ReservationId], context.CancellationToken);
        if (reservation is null || reservation.Status == ReservationStatus.Rejected)
        {
            return; // unknown or already applied — idempotent no-op
        }

        reservation.Status = ReservationStatus.Rejected;
        reservation.RejectionReason = msg.Reason;
        reservation.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(context.CancellationToken);

        await context.Publish(new ReservationRejected
        {
            ReservationId = reservation.Id,
            CustomerEmail = reservation.CustomerEmail,
            Reason = msg.Reason
        });
    }
}
