using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Infrastructure.Context;

public class DBContext : DbContext
{
    public DbSet<AccountProjection> AccountProjection { get; set; }
    public DbSet<Transfer> Transfer { get; set; }

    public DBContext(DbContextOptions<DBContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AccountProjection>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Iban).HasMaxLength(26).IsRequired();
            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            builder.Property(x => x.Status).HasConversion<int>();
            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.Iban);
        });

        modelBuilder.Entity<Transfer>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(256).IsRequired();
            builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            builder.Property(x => x.RejectReason).HasMaxLength(512);
            builder.Property(x => x.Amount).HasPrecision(19, 4);
            builder.Property(x => x.Status).HasConversion<int>();
            builder.HasIndex(x => new { x.CustomerId, x.IdempotencyKey }).IsUnique();
            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.Status);
        });

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }

    public async Task<int> CompleteSaveAsync(CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return await SaveChangesAsync(cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
                entry.Entity.CreatedAt = now;

            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }
}
