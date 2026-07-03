using MatchService.Domain;
using Microsoft.EntityFrameworkCore;

namespace MatchService.Data;

public class MatchDbContext(DbContextOptions<MatchDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Stadium> Stadiums => Set<Stadium>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Sector> Sectors => Set<Sector>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(100);
            e.Property(t => t.ShortCode).HasMaxLength(10);
            e.Property(t => t.City).HasMaxLength(100);
        });

        modelBuilder.Entity<Stadium>(e =>
        {
            e.Property(s => s.Name).HasMaxLength(150);
            e.Property(s => s.City).HasMaxLength(100);
        });

        modelBuilder.Entity<Match>(e =>
        {
            // Teams are reference data — block deleting a team that has matches.
            e.HasOne(m => m.HomeTeam).WithMany()
                .HasForeignKey(m => m.HomeTeamId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.AwayTeam).WithMany()
                .HasForeignKey(m => m.AwayTeamId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Stadium).WithMany()
                .HasForeignKey(m => m.StadiumId).OnDelete(DeleteBehavior.Restrict);
            // Sectors are owned by the match — remove them with it.
            e.HasMany(m => m.Sectors).WithOne(s => s.Match)
                .HasForeignKey(s => s.MatchId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Sector>(e =>
        {
            e.Property(s => s.Name).HasMaxLength(100);
            e.Property(s => s.Price).HasPrecision(10, 2);
        });
    }
}
