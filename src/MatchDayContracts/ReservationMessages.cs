namespace MatchDayContracts;

// The Phase 1 reservation flow (RabbitMQ / MassTransit):
//
//   ReservationService --(ReserveSeats)-->            MatchService
//   MatchService       --(SeatsReserved |             --> ReservationService
//                         SeatsReservationRejected)
//   ReservationService --(ReservationConfirmed |      --> NotificationService
//                         ReservationRejected)
//
// MatchService owns seat availability, so it is the one that atomically holds the
// seats (guarding against oversell and duplicate ReserveSeats deliveries).

/// <summary>Command: ask MatchService to hold seats for a reservation.</summary>
public record ReserveSeats
{
    public Guid ReservationId { get; init; }
    public Guid MatchId { get; init; }
    public Guid SectorId { get; init; }
    public int Quantity { get; init; }
}

/// <summary>Event: MatchService successfully held the seats.</summary>
public record SeatsReserved
{
    public Guid ReservationId { get; init; }
    public Guid MatchId { get; init; }
    public Guid SectorId { get; init; }
    public int Quantity { get; init; }
    public decimal TotalPrice { get; init; }
}

/// <summary>Event: MatchService could not hold the seats (sold out, unknown match/sector, …).</summary>
public record SeatsReservationRejected
{
    public Guid ReservationId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

/// <summary>Event: the reservation is confirmed — seats are held. Consumed by NotificationService.</summary>
public record ReservationConfirmed
{
    public Guid ReservationId { get; init; }
    public Guid MatchId { get; init; }
    public Guid SectorId { get; init; }
    public int Quantity { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
}

/// <summary>Event: the reservation failed. Consumed by NotificationService.</summary>
public record ReservationRejected
{
    public Guid ReservationId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
