using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MatchService.Data;

/// <summary>
/// Used by the EF Core CLI (`dotnet ef migrations` / `database update`) at design time,
/// so the tools never execute the app's startup migrate/seed code. The connection string
/// only needs to be valid for scaffolding; it points at the local Docker Postgres.
/// </summary>
public class MatchDbContextFactory : IDesignTimeDbContextFactory<MatchDbContext>
{
    public MatchDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MatchDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=matchdb;Username=matchday;Password=matchday")
            .Options;

        return new MatchDbContext(options);
    }
}
