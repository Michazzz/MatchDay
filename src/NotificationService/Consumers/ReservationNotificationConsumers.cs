using MassTransit;
using MatchDayContracts;

namespace NotificationService.Consumers;

/// <summary>Reservation confirmed → "send" the customer a confirmation (logged for the demo).</summary>
public class ReservationConfirmedConsumer(ILogger<ReservationConfirmedConsumer> logger)
    : IConsumer<ReservationConfirmed>
{
    public Task Consume(ConsumeContext<ReservationConfirmed> context)
    {
        var m = context.Message;
        logger.LogInformation(
            "📧 Reservation {ReservationId} confirmed — notifying {Email}: {Quantity} seat(s) reserved.",
            m.ReservationId, m.CustomerEmail, m.Quantity);
        return Task.CompletedTask;
    }
}

/// <summary>Reservation rejected → "send" the customer the bad news (logged for the demo).</summary>
public class ReservationRejectedConsumer(ILogger<ReservationRejectedConsumer> logger)
    : IConsumer<ReservationRejected>
{
    public Task Consume(ConsumeContext<ReservationRejected> context)
    {
        var m = context.Message;
        logger.LogInformation(
            "📧 Reservation {ReservationId} rejected — notifying {Email}: {Reason}",
            m.ReservationId, m.CustomerEmail, m.Reason);
        return Task.CompletedTask;
    }
}
