using RiskService.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace RiskService.Infrastructure.Context;

public class DBContext : DbContext
{
    public DbSet<AccountProjection> AccountProjection { get; set; }
    public DbSet<RiskScreening> RiskScreening { get; set; }
    public DbSet<RiskAlert> RiskAlert { get; set; }
    public DbSet<CustomerProfile> CustomerProfile { get; set; }

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
            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            builder.HasIndex(x => x.CustomerId);
        });

        modelBuilder.Entity<RiskScreening>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            builder.Property(x => x.Amount).HasPrecision(19, 4);
            builder.Property(x => x.Channel).HasConversion<int>();
            builder.Property(x => x.Direction).HasConversion<int>();
            builder.HasIndex(x => new { x.Channel, x.SourceId }).IsUnique();
            builder.HasIndex(x => x.CustomerId);
        });

        modelBuilder.Entity<RiskAlert>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Rule).HasMaxLength(32).IsRequired();
            builder.Property(x => x.CloseReason).HasMaxLength(512);
            builder.Property(x => x.Amount).HasPrecision(19, 4);
            builder.Property(x => x.Channel).HasConversion<int>();
            builder.Property(x => x.Status).HasConversion<int>();
            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<CustomerProfile>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CifNumber).HasMaxLength(16).IsRequired();
            builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.NationalId).HasMaxLength(11);
            builder.HasIndex(x => x.NationalId);
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
