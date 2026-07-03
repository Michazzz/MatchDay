using MatchService.Domain;

namespace MatchService.Contracts;

// --- Responses ---

public record TeamDto(Guid Id, string Name, string ShortCode, string City);

public record StadiumDto(Guid Id, string Name, string City, int Capacity);

public record SectorDto(Guid Id, string Name, int Capacity, int AvailableSeats, decimal Price);

public record MatchSummaryDto(
    Guid Id,
    string HomeTeam,
    string AwayTeam,
    string Stadium,
    DateTimeOffset KickoffUtc,
    MatchStatus Status);

public record MatchDetailsDto(
    Guid Id,
    TeamDto HomeTeam,
    TeamDto AwayTeam,
    StadiumDto Stadium,
    DateTimeOffset KickoffUtc,
    MatchStatus Status,
    IReadOnlyList<SectorDto> Sectors);

// --- Requests ---

public record CreateSectorRequest(string Name, int Capacity, decimal Price);

public record CreateMatchRequest(
    Guid HomeTeamId,
    Guid AwayTeamId,
    Guid StadiumId,
    DateTimeOffset KickoffUtc,
    IReadOnlyList<CreateSectorRequest> Sectors);

public record UpdateMatchRequest(DateTimeOffset KickoffUtc, MatchStatus Status);
