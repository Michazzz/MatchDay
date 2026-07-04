using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ReservationService.Data;

/// <summary>Design-time factory for the EF Core CLI (points at the local Docker Postgres).</summary>
public class ReservationDbContextFactory : IDesignTimeDbContextFactory<ReservationDbContext>
{
    public ReservationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ReservationDbContext>()
            .UseNpgsql("Host=localhost;Port=5434;Database=reservationdb;Username=matchday;Password=matchday")
            .Options;

        return new ReservationDbContext(options);
    }
}
