using MatchService.Contracts;
using MatchService.Data;
using MatchService.Domain;
using Microsoft.EntityFrameworkCore;

namespace MatchService.Endpoints;

public static class MatchEndpoints
{
    public static void MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/matches").WithTags("Matches");

        group.MapGet("/", GetMatches);
        group.MapGet("/{id:guid}", GetMatch);
        group.MapPost("/", CreateMatch);
        group.MapPut("/{id:guid}", UpdateMatch);
        group.MapDelete("/{id:guid}", DeleteMatch);
        group.MapGet("/{id:guid}/sectors", GetSectors);
    }

    public static void MapReferenceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/teams", async (MatchDbContext db, CancellationToken ct) =>
            await db.Teams.OrderBy(t => t.Name)
                .Select(t => new TeamDto(t.Id, t.Name, t.ShortCode, t.City))
                .ToListAsync(ct))
            .WithTags("Reference");

        app.MapGet("/stadiums", async (MatchDbContext db, CancellationToken ct) =>
            await db.Stadiums.OrderBy(s => s.Name)
                .Select(s => new StadiumDto(s.Id, s.Name, s.City, s.Capacity))
                .ToListAsync(ct))
            .WithTags("Reference");
    }

    private static async Task<IResult> GetMatches(MatchDbContext db, CancellationToken ct)
    {
        var matches = await db.Matches
            .OrderBy(m => m.KickoffUtc)
            .Select(m => new MatchSummaryDto(
                m.Id, m.HomeTeam.Name, m.AwayTeam.Name, m.Stadium.Name, m.KickoffUtc, m.Status))
            .ToListAsync(ct);

        return Results.Ok(matches);
    }

    private static async Task<IResult> GetMatch(Guid id, MatchDbContext db, CancellationToken ct)
    {
        var match = await db.Matches
            .Where(m => m.Id == id)
            .Select(m => new MatchDetailsDto(
                m.Id,
                new TeamDto(m.HomeTeam.Id, m.HomeTeam.Name, m.HomeTeam.ShortCode, m.HomeTeam.City),
                new TeamDto(m.AwayTeam.Id, m.AwayTeam.Name, m.AwayTeam.ShortCode, m.AwayTeam.City),
                new StadiumDto(m.Stadium.Id, m.Stadium.Name, m.Stadium.City, m.Stadium.Capacity),
                m.KickoffUtc,
                m.Status,
                m.Sectors
                    .OrderBy(s => s.Name)
                    .Select(s => new SectorDto(s.Id, s.Name, s.Capacity, s.AvailableSeats, s.Price))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

        return match is null ? Results.NotFound() : Results.Ok(match);
    }

    private static async Task<IResult> CreateMatch(CreateMatchRequest req, MatchDbContext db, CancellationToken ct)
    {
        if (req.HomeTeamId == req.AwayTeamId)
        {
            return Problem("Home and away team must be different.");
        }

        if (req.Sectors is null || req.Sectors.Count == 0)
        {
            return Problem("At least one sector is required.");
        }

        var knownTeams = await db.Teams
            .CountAsync(t => t.Id == req.HomeTeamId || t.Id == req.AwayTeamId, ct);
        if (knownTeams < 2)
        {
            return Problem("Home or away team does not exist.");
        }

        if (!await db.Stadiums.AnyAsync(s => s.Id == req.StadiumId, ct))
        {
            return Problem("Stadium does not exist.");
        }

        var match = new Match
        {
            Id = Guid.NewGuid(),
            HomeTeamId = req.HomeTeamId,
            AwayTeamId = req.AwayTeamId,
            StadiumId = req.StadiumId,
            KickoffUtc = req.KickoffUtc,
            Status = MatchStatus.Scheduled,
            Sectors = req.Sectors.Select(s => new Sector
            {
                Id = Guid.NewGuid(),
                Name = s.Name,
                Capacity = s.Capacity,
                AvailableSeats = s.Capacity,
                Price = s.Price
            }).ToList()
        };

        db.Matches.Add(match);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/matches/{match.Id}", new { match.Id });
    }

    private static async Task<IResult> UpdateMatch(Guid id, UpdateMatchRequest req, MatchDbContext db, CancellationToken ct)
    {
        var match = await db.Matches.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (match is null)
        {
            return Results.NotFound();
        }

        match.KickoffUtc = req.KickoffUtc;
        match.Status = req.Status;
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteMatch(Guid id, MatchDbContext db, CancellationToken ct)
    {
        var match = await db.Matches.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (match is null)
        {
            return Results.NotFound();
        }

        db.Matches.Remove(match);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    private static async Task<IResult> GetSectors(Guid id, MatchDbContext db, CancellationToken ct)
    {
        if (!await db.Matches.AnyAsync(m => m.Id == id, ct))
        {
            return Results.NotFound();
        }

        var sectors = await db.Sectors
            .Where(s => s.MatchId == id)
            .OrderBy(s => s.Name)
            .Select(s => new SectorDto(s.Id, s.Name, s.Capacity, s.AvailableSeats, s.Price))
            .ToListAsync(ct);

        return Results.Ok(sectors);
    }

    private static IResult Problem(string detail) =>
        Results.Problem(detail, statusCode: StatusCodes.Status400BadRequest);
}
