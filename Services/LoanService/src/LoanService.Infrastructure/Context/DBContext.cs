using LoanService.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace LoanService.Infrastructure.Context;

public class DBContext : DbContext
{
    public DbSet<AccountProjection> AccountProjection { get; set; }
    public DbSet<Loan> Loan { get; set; }
    public DbSet<LoanInstallment> LoanInstallment { get; set; }

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
            builder.Property(x => x.Status).HasConversion<int>();
            builder.HasIndex(x => x.CustomerId);
        });

        modelBuilder.Entity<Loan>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            builder.Property(x => x.RejectReason).HasMaxLength(512);
            builder.Property(x => x.Amount).HasPrecision(19, 4);
            builder.Property(x => x.RemainingPrincipal).HasPrecision(19, 4);
            builder.Property(x => x.Status).HasConversion<int>();
            builder.HasIndex(x => new { x.CustomerId, x.IdempotencyKey }).IsUnique();
            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.AccountId);
            builder.HasIndex(x => x.Status);
            builder.HasMany(x => x.Installments)
                .WithOne(x => x.Loan)
                .HasForeignKey(x => x.LoanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LoanInstallment>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Amount).HasPrecision(19, 4);
            builder.Property(x => x.Status).HasConversion<int>();
            builder.HasIndex(x => new { x.LoanId, x.Sequence }).IsUnique();
            builder.HasIndex(x => x.LoanId);
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
