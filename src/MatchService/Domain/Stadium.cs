namespace MatchService.Domain;

public class Stadium
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string City { get; set; }
    public int Capacity { get; set; }
}
