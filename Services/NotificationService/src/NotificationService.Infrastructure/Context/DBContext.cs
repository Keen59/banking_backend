using NotificationService.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace NotificationService.Infrastructure.Context;

public class DBContext : DbContext
{
    public DbSet<CustomerNotification> CustomerNotification { get; set; }

    public DBContext(DbContextOptions<DBContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CustomerNotification>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).HasMaxLength(64).IsRequired();
            builder.Property(x => x.Title).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Body).HasMaxLength(512).IsRequired();
            builder.HasIndex(x => new { x.Type, x.SourceId }).IsUnique();
            builder.HasIndex(x => x.CustomerId);
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
