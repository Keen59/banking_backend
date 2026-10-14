using OpenBankingService.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace OpenBankingService.Infrastructure.Context;

public class DBContext : DbContext
{
    public DbSet<AccountProjection> AccountProjection { get; set; }
    public DbSet<Consent> Consent { get; set; }
    public DbSet<ConsentAccount> ConsentAccount { get; set; }
    public DbSet<PaymentInitiation> PaymentInitiation { get; set; }

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

        modelBuilder.Entity<Consent>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TppClientId).HasMaxLength(64).IsRequired();
            builder.Property(x => x.AccessToken).HasMaxLength(32).IsRequired();
            builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Status).HasConversion<int>();
            builder.HasIndex(x => x.AccessToken).IsUnique();
            builder.HasIndex(x => new { x.CustomerId, x.IdempotencyKey }).IsUnique();
            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.Status);
            builder.HasMany(x => x.Accounts)
                .WithOne(x => x.Consent)
                .HasForeignKey(x => x.ConsentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConsentAccount>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => new { x.ConsentId, x.AccountId }).IsUnique();
            builder.HasIndex(x => x.ConsentId);
            builder.HasIndex(x => x.AccountId);
        });

        modelBuilder.Entity<PaymentInitiation>(builder =>
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
            builder.HasIndex(x => x.ConsentId);
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
