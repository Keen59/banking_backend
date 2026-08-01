using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Context
{
    public class DBContext : DbContext
    {

        public DbSet<User> User { get; set; }
        public DbSet<AuditLog> AuditLog { get; set; }
        public DbSet<Device> Device { get; set; }
        public DbSet<LoginAttempt> LoginAttempt { get; set; }
        public DbSet<Permission> Permission { get; set; }
        public DbSet<RefreshToken> RefreshToken { get; set; }
        public DbSet<Role> Role { get; set; }
        public DbSet<RolePermission> RolePermission { get; set; }
        public DbSet<UserRole> UserRole { get; set; }
        public DbSet<UserSession> UserSession { get; set; }


        public DBContext(DbContextOptions options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?));

                foreach (var property in properties)
                {
                    modelBuilder.Entity(entityType.Name)
                        .Property(property.Name)
                        .HasColumnType("timestamp without time zone");
                }
            }
            #region User

            modelBuilder.Entity<User>(builder =>
            {
                builder.HasKey(x => x.Id);

                builder.HasIndex(x => x.Email).IsUnique();
                builder.HasIndex(x => x.Username).IsUnique();

                builder.Property(x => x.Email)
                    .HasMaxLength(256)
                    .IsRequired();

                builder.Property(x => x.Username)
                    .HasMaxLength(50)
                    .IsRequired();

                builder.Property(x => x.PhoneNumber)
                    .HasMaxLength(20);

                builder.Property(x => x.PasswordHash)
                    .HasMaxLength(512)
                    .IsRequired();

                builder.Property(x => x.Status)
                    .HasConversion<int>();

                builder.HasMany(x => x.RefreshTokens)
                    .WithOne(x => x.User)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasMany(x => x.Devices)
                    .WithOne(x => x.User)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasMany(x => x.Sessions)
                    .WithOne(x => x.User)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasMany(x => x.LoginAttempts)
                    .WithOne(x => x.User)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                builder.HasMany(x => x.AuditLogs)
                    .WithOne(x => x.User)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            #endregion

            #region Role

            modelBuilder.Entity<Role>(builder =>
            {
                builder.HasKey(x => x.Id);

                builder.Property(x => x.Name)
                    .HasMaxLength(50)
                    .IsRequired();

                builder.Property(x => x.Description)
                    .HasMaxLength(250);

                builder.HasIndex(x => x.Name)
                    .IsUnique();
            });

            #endregion

            #region Permission

            modelBuilder.Entity<Permission>(builder =>
            {
                builder.HasKey(x => x.Id);

                builder.Property(x => x.Code)
                    .HasMaxLength(100)
                    .IsRequired();

                builder.Property(x => x.Description)
                    .HasMaxLength(250);

                builder.HasIndex(x => x.Code)
                    .IsUnique();
            });

            #endregion

            #region UserRole

            modelBuilder.Entity<UserRole>(builder =>
            {
                builder.HasKey(x => new
                {
                    x.UserId,
                    x.RoleId
                });

                builder.HasOne(x => x.User)
                    .WithMany(x => x.UserRoles)
                    .HasForeignKey(x => x.UserId);

                builder.HasOne(x => x.Role)
                    .WithMany(x => x.UserRoles)
                    .HasForeignKey(x => x.RoleId);
            });

            #endregion

            #region RolePermission

            modelBuilder.Entity<RolePermission>(builder =>
            {
                builder.HasKey(x => new
                {
                    x.RoleId,
                    x.PermissionId
                });

                builder.HasOne(x => x.Role)
                    .WithMany(x => x.RolePermissions)
                    .HasForeignKey(x => x.RoleId);

                builder.HasOne(x => x.Permission)
                    .WithMany(x => x.RolePermissions)
                    .HasForeignKey(x => x.PermissionId);
            });

            #endregion

            #region RefreshToken

            modelBuilder.Entity<RefreshToken>(builder =>
            {
                builder.HasKey(x => x.Id);

                builder.Property(x => x.Token)
                    .HasMaxLength(512)
                    .IsRequired();

                builder.Property(x => x.CreatedIp)
                    .HasMaxLength(45);

                builder.Property(x => x.RevokedIp)
                    .HasMaxLength(45);

                builder.HasIndex(x => x.Token)
                    .IsUnique();
            });

            #endregion

            #region Device

            modelBuilder.Entity<Device>(builder =>
            {
                builder.HasKey(x => x.Id);

                builder.Property(x => x.DeviceIdentifier)
                    .HasMaxLength(256);

                builder.Property(x => x.DeviceName)
                    .HasMaxLength(100);

                builder.Property(x => x.OperatingSystem)
                    .HasMaxLength(100);

                builder.Property(x => x.Browser)
                    .HasMaxLength(100);

                builder.Property(x => x.IpAddress)
                    .HasMaxLength(45);

                builder.HasIndex(x => x.DeviceIdentifier);
            });

            #endregion

            #region UserSession

            modelBuilder.Entity<UserSession>(builder =>
            {
                builder.HasKey(x => x.Id);

                builder.Property(x => x.IpAddress)
                    .HasMaxLength(45);

                builder.HasOne(x => x.RefreshToken)
                    .WithMany()
                    .HasForeignKey(x => x.RefreshTokenId);

                builder.HasIndex(x => x.JwtId);

                builder.HasIndex(x => x.IsActive);
            });

            #endregion

            #region LoginAttempt

            modelBuilder.Entity<LoginAttempt>(builder =>
            {
                builder.HasKey(x => x.Id);

                builder.Property(x => x.Email)
                    .HasMaxLength(256);

                builder.Property(x => x.IpAddress)
                    .HasMaxLength(45);

                builder.Property(x => x.Device)
                    .HasMaxLength(100);

                builder.Property(x => x.FailureReason)
                    .HasConversion<int>();

                builder.HasIndex(x => x.Email);

                builder.HasIndex(x => x.CreatedAt);

                builder.HasIndex(x => x.IpAddress);
            });

            #endregion

            #region AuditLog

            modelBuilder.Entity<AuditLog>(builder =>
            {
                builder.HasKey(x => x.Id);

                builder.Property(x => x.Resource)
                    .HasMaxLength(100);

                builder.Property(x => x.IpAddress)
                    .HasMaxLength(45);

                builder.Property(x => x.Device)
                    .HasMaxLength(100);

                builder.Property(x => x.Metadata)
                    .HasColumnType("jsonb");

                builder.Property(x => x.Action)
                    .HasConversion<int>();

                builder.HasIndex(x => x.UserId);

                builder.HasIndex(x => x.Action);

                builder.HasIndex(x => x.CreatedAt);
            });

            #endregion
        }
     
        public async Task<int> CompleteSaveAsync()
        {
            ApplyTimestamps();
            return await SaveChangesAsync();
        }

        public int CompleteSave()
        {
            ApplyTimestamps();
            return SaveChanges();
        }

        private void ApplyTimestamps()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.Now;
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = DateTime.Now;
                }
            }
        }
    }
}
