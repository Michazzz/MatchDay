namespace ReservationService.Domain;

public enum ReservationStatus
{
    Pending = 0,
    Reserved = 1,
    Rejected = 2
}

public class Reservation
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public Guid SectorId { get; set; }
    public int Quantity { get; set; }
    public required string CustomerEmail { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public string? RejectionReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
