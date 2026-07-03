namespace MatchService.Domain;

public class Team
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string ShortCode { get; set; }
    public required string City { get; set; }
}
