using Microsoft.EntityFrameworkCore;
using ReservationService.Domain;

namespace ReservationService.Data;

public class ReservationDbContext(DbContextOptions<ReservationDbContext> options) : DbContext(options)
{
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reservation>(e =>
        {
            e.Property(r => r.CustomerEmail).HasMaxLength(256);
            e.Property(r => r.RejectionReason).HasMaxLength(500);
            e.HasIndex(r => r.Status);
        });
    }
}
