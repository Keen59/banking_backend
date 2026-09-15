using CustomerService.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Infrastructure.Context;

public class DBContext : DbContext
{
    public DbSet<Customer> Customer { get; set; }
    public DbSet<CustomerAddress> CustomerAddress { get; set; }
    public DbSet<KycDocument> KycDocument { get; set; }
    public DbSet<CustomerConsent> CustomerConsent { get; set; }
    public DbSet<AuditLog> AuditLog { get; set; }

    public DBContext(DbContextOptions<DBContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(builder =>
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.CifNumber)
                .HasMaxLength(16)
                .IsRequired();

            builder.HasIndex(x => x.CifNumber).IsUnique();

            builder.Property(x => x.NationalId)
                .HasMaxLength(11);

            builder.HasIndex(x => x.NationalId)
                .IsUnique()
                .HasFilter("\"NationalId\" IS NOT NULL");

            builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Nationality).HasMaxLength(2).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
            builder.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
            builder.Property(x => x.KycRejectReason).HasMaxLength(500);

            builder.Property(x => x.Type).HasConversion<int>();
            builder.Property(x => x.Status).HasConversion<int>();
            builder.Property(x => x.KycStatus).HasConversion<int>();
            builder.Property(x => x.KycLevel).HasConversion<int>();

            builder.HasMany(x => x.Addresses)
                .WithOne(x => x.Customer)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Documents)
                .WithOne(x => x.Customer)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Consents)
                .WithOne(x => x.Customer)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.AuditLogs)
                .WithOne(x => x.Customer)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<CustomerAddress>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).HasConversion<int>();
            builder.Property(x => x.Country).HasMaxLength(2).IsRequired();
            builder.Property(x => x.City).HasMaxLength(100).IsRequired();
            builder.Property(x => x.District).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Line1).HasMaxLength(250).IsRequired();
            builder.Property(x => x.PostalCode).HasMaxLength(10);
            builder.HasIndex(x => x.CustomerId);
        });

        modelBuilder.Entity<KycDocument>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DocumentType).HasConversion<int>();
            builder.Property(x => x.Status).HasConversion<int>();
            builder.Property(x => x.FileReference).HasMaxLength(500).IsRequired();
            builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
            builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            builder.Property(x => x.RejectReason).HasMaxLength(500);
            builder.HasIndex(x => x.CustomerId);
        });

        modelBuilder.Entity<CustomerConsent>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).HasConversion<int>();
            builder.Property(x => x.IpAddress).HasMaxLength(45);
            builder.HasIndex(x => new { x.CustomerId, x.Type });
        });

        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Action).HasConversion<int>();
            builder.Property(x => x.Resource).HasMaxLength(100).IsRequired();
            builder.Property(x => x.IpAddress).HasMaxLength(45);
            builder.Property(x => x.Metadata).HasColumnType("jsonb");
            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.Action);
            builder.HasIndex(x => x.CreatedAt);
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
        var entries = ChangeTracker.Entries<BaseEntity>();
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;

            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }
}
