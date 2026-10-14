using CardService.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CardService.Infrastructure.Context;

public class DBContext : DbContext
{
    public DbSet<Card> Card { get; set; }
    public DbSet<CardAuthorization> CardAuthorization { get; set; }

    public DBContext(DbContextOptions<DBContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Card>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Token).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Last4).HasMaxLength(4).IsRequired();
            builder.Property(x => x.Brand).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Status).HasConversion<int>();
            builder.HasIndex(x => x.Token).IsUnique();
            builder.HasIndex(x => x.AccountId).IsUnique();
            builder.HasIndex(x => x.CustomerId);
        });

        modelBuilder.Entity<CardAuthorization>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            builder.Property(x => x.Merchant).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(256).IsRequired();
            builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            builder.Property(x => x.RejectReason).HasMaxLength(512);
            builder.Property(x => x.Amount).HasPrecision(19, 4);
            builder.Property(x => x.Status).HasConversion<int>();
            builder.HasIndex(x => new { x.CustomerId, x.IdempotencyKey }).IsUnique();
            builder.HasIndex(x => x.CardId);
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
