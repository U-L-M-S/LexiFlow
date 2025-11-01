using LexiFlow.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LexiFlow.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>()
            .HasIndex(user => user.Username)
            .IsUnique();

        modelBuilder.Entity<Receipt>()
            .Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        modelBuilder.Entity<Receipt>()
            .Property(r => r.Currency)
            .HasMaxLength(8)
            .HasDefaultValue("EUR");

        modelBuilder.Entity<Receipt>()
            .HasOne(r => r.CreatedBy)
            .WithMany(u => u.Receipts)
            .HasForeignKey(r => r.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Receipt)
            .WithOne(r => r.Booking)
            .HasForeignKey<Booking>(b => b.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
