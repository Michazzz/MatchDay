using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;

namespace PaymentService.Data;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(e =>
        {
            e.HasKey(p => p.ReservationId);
            e.Property(p => p.TotalPrice).HasPrecision(10, 2);
            e.Property(p => p.FailureReason).HasMaxLength(500);
            e.HasIndex(p => p.Status);
        });
    }
}
