using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace LedgerService.Infrastructure.Context;

public class DBContext : DbContext
{
    public DbSet<LedgerAccount> LedgerAccount { get; set; }
    public DbSet<JournalEntry> JournalEntry { get; set; }
    public DbSet<JournalLine> JournalLine { get; set; }
    public DbSet<AccountHold> AccountHold { get; set; }

    public DBContext(DbContextOptions<DBContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LedgerAccount>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            builder.Property(x => x.Kind).HasConversion<int>();
            builder.Property(x => x.Status).HasConversion<int>();
            builder.HasIndex(x => x.SourceAccountId)
                .IsUnique()
                .HasFilter("\"SourceAccountId\" IS NOT NULL");
            builder.HasIndex(x => x.CustomerId);

            builder.HasData(new LedgerAccount
            {
                Id = SystemLedgerAccounts.InternalClearingTry,
                SourceAccountId = null,
                CustomerId = null,
                Currency = "TRY",
                Kind = ELedgerAccountKind.InternalClearing,
                Status = ELedgerAccountStatus.Active,
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            });
        });

        modelBuilder.Entity<JournalEntry>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(256).IsRequired();
            builder.HasIndex(x => x.IdempotencyKey).IsUnique();
            builder.HasMany(x => x.Lines)
                .WithOne(x => x.JournalEntry)
                .HasForeignKey(x => x.JournalEntryId);
        });

        modelBuilder.Entity<JournalLine>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Side).HasConversion<int>();
            builder.Property(x => x.Amount).HasPrecision(19, 4);
            builder.HasOne(x => x.LedgerAccount)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.LedgerAccountId);
            builder.HasIndex(x => x.LedgerAccountId);
        });

        modelBuilder.Entity<AccountHold>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.IdempotencyKey).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Amount).HasPrecision(19, 4);
            builder.Property(x => x.Status).HasConversion<int>();
            builder.HasIndex(x => x.IdempotencyKey).IsUnique();
            builder.HasOne(x => x.LedgerAccount)
                .WithMany(x => x.Holds)
                .HasForeignKey(x => x.LedgerAccountId);
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
