using System.Text.Json;
using MatchService.Domain;
using Microsoft.EntityFrameworkCore;

namespace MatchService.Data;

/// <summary>
/// Seeds a small Ekstraklasa-flavoured catalog on first run so the service and the
/// (future) frontend have something to show without manual data entry.
///
/// The data lives in <c>Data/seed-data.json</c> (copied to the output directory) to
/// keep the catalog separate from code and easy to edit or extend.
/// </summary>
public static class MatchSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task SeedAsync(MatchDbContext db, CancellationToken ct = default)
    {
        if (await db.Teams.AnyAsync(ct))
        {
            return;
        }

        var seed = await LoadSeedDataAsync(ct);

        var teamsByCode = seed.Teams.ToDictionary(
            t => t.ShortCode,
            t => new Team { Id = Guid.NewGuid(), Name = t.Name, ShortCode = t.ShortCode, City = t.City });

        var stadiumsByName = seed.Stadiums.ToDictionary(
            s => s.Name,
            s => new Stadium { Id = Guid.NewGuid(), Name = s.Name, City = s.City, Capacity = s.Capacity });

        db.Teams.AddRange(teamsByCode.Values);
        db.Stadiums.AddRange(stadiumsByName.Values);

        foreach (var m in seed.Matches)
        {
            db.Matches.Add(new Match
            {
                Id = Guid.NewGuid(),
                HomeTeamId = teamsByCode[m.HomeTeam].Id,
                AwayTeamId = teamsByCode[m.AwayTeam].Id,
                StadiumId = stadiumsByName[m.Stadium].Id,
                KickoffUtc = m.KickoffUtc,
                Status = MatchStatus.Scheduled,
                Sectors = m.Sectors.Select(s => new Sector
                {
                    Id = Guid.NewGuid(),
                    Name = s.Name,
                    Capacity = s.Capacity,
                    AvailableSeats = s.Capacity,
                    Price = s.Price
                }).ToList()
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task<SeedData> LoadSeedDataAsync(CancellationToken ct)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "seed-data.json");
        var bytes = await File.ReadAllBytesAsync(path, ct);
        return JsonSerializer.Deserialize<SeedData>(bytes, JsonOptions)
            ?? throw new InvalidOperationException($"Seed file '{path}' deserialized to null.");
    }

    private sealed record SeedData(
        List<SeedTeam> Teams,
        List<SeedStadium> Stadiums,
        List<SeedMatch> Matches);

    private sealed record SeedTeam(string Name, string ShortCode, string City);

    private sealed record SeedStadium(string Name, string City, int Capacity);

    private sealed record SeedMatch(
        string HomeTeam,
        string AwayTeam,
        string Stadium,
        DateTimeOffset KickoffUtc,
        List<SeedSector> Sectors);

    private sealed record SeedSector(string Name, int Capacity, decimal Price);
}
