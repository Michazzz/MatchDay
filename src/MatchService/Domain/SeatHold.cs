namespace MatchService.Domain;

/// <summary>
/// Record of seats held for a reservation. Keyed by ReservationId so a redelivered
/// <c>ReserveSeats</c> command is processed exactly once (idempotency).
/// </summary>
public class SeatHold
{
    public Guid ReservationId { get; set; }
    public Guid SectorId { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset HeldAt { get; set; }
}
