using MassTransit;
using MatchDayContracts;
using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Domain;

namespace ReservationService.Endpoints;

public record CreateReservationRequest(Guid MatchId, Guid SectorId, int Quantity, string CustomerEmail);

public record ReservationResponse(
    Guid Id,
    Guid MatchId,
    Guid SectorId,
    int Quantity,
    string CustomerEmail,
    ReservationStatus Status,
    string? RejectionReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public static class ReservationEndpoints
{
    public static void MapReservationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/reservations").WithTags("Reservations");

        group.MapPost("/", CreateReservation);
        group.MapGet("/{id:guid}", GetReservation);
        group.MapGet("/", GetReservations);
    }

    private static async Task<IResult> CreateReservation(
        CreateReservationRequest req,
        ReservationDbContext db,
        IPublishEndpoint publish,
        CancellationToken ct)
    {
        if (req.Quantity <= 0)
        {
            return Problem("Quantity must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(req.CustomerEmail))
        {
            return Problem("CustomerEmail is required.");
        }

        var now = DateTimeOffset.UtcNow;
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            MatchId = req.MatchId,
            SectorId = req.SectorId,
            Quantity = req.Quantity,
            CustomerEmail = req.CustomerEmail,
            Status = ReservationStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Reservations.Add(reservation);
        await db.SaveChangesAsync(ct);

        // Hand off to MatchService (owner of seat availability) over the bus.
        await publish.Publish(new ReserveSeats
        {
            ReservationId = reservation.Id,
            MatchId = reservation.MatchId,
            SectorId = reservation.SectorId,
            Quantity = reservation.Quantity
        }, ct);

        // 202: accepted for async processing; poll GET /reservations/{id} for the outcome.
        return Results.Accepted($"/reservations/{reservation.Id}", ToResponse(reservation));
    }

    private static async Task<IResult> GetReservation(Guid id, ReservationDbContext db, CancellationToken ct)
    {
        var reservation = await db.Reservations.FindAsync([id], ct);
        return reservation is null ? Results.NotFound() : Results.Ok(ToResponse(reservation));
    }

    private static async Task<IResult> GetReservations(ReservationDbContext db, CancellationToken ct)
    {
        var reservations = await db.Reservations
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToResponse(r))
            .ToListAsync(ct);

        return Results.Ok(reservations);
    }

    private static ReservationResponse ToResponse(Reservation r) => new(
        r.Id, r.MatchId, r.SectorId, r.Quantity, r.CustomerEmail,
        r.Status, r.RejectionReason, r.CreatedAt, r.UpdatedAt);

    private static IResult Problem(string detail) =>
        Results.Problem(detail, statusCode: StatusCodes.Status400BadRequest);
}
