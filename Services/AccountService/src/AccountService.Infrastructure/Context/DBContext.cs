using AccountService.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Infrastructure.Context;

public class DBContext : DbContext
{
    public DbSet<Account> Account { get; set; }

    public DBContext(DbContextOptions<DBContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(builder =>
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CifNumber).HasMaxLength(16).IsRequired();
            builder.Property(x => x.Iban).HasMaxLength(26).IsRequired();
            builder.Property(x => x.AccountNumber).HasMaxLength(16).IsRequired();
            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            builder.Property(x => x.ProductType).HasConversion<int>();
            builder.Property(x => x.Status).HasConversion<int>();

            builder.HasIndex(x => x.Iban).IsUnique();
            builder.HasIndex(x => x.AccountNumber).IsUnique();
            builder.HasIndex(x => new { x.CustomerId, x.ProductType, x.Currency }).IsUnique();
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
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;

            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }
}
