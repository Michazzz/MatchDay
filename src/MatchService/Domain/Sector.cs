namespace MatchService.Domain;

/// <summary>
/// Per-match ticketing sector. Availability is tracked here and decremented by
/// the (future) ReservationService when seats are reserved.
/// </summary>
public class Sector
{
    public Guid Id { get; set; }

    public Guid MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public required string Name { get; set; }
    public int Capacity { get; set; }
    public int AvailableSeats { get; set; }
    public decimal Price { get; set; }
}
