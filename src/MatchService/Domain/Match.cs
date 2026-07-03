namespace MatchService.Domain;

public class Match
{
    public Guid Id { get; set; }

    public Guid HomeTeamId { get; set; }
    public Team HomeTeam { get; set; } = null!;

    public Guid AwayTeamId { get; set; }
    public Team AwayTeam { get; set; } = null!;

    public Guid StadiumId { get; set; }
    public Stadium Stadium { get; set; } = null!;

    public DateTimeOffset KickoffUtc { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;

    public List<Sector> Sectors { get; set; } = [];
}
