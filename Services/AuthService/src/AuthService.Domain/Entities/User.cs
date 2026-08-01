using AuthService.Domain.Enums;

namespace AuthService.Domain.Entities
{
    public class User : BaseEntity
    {
        public Guid CustomerId { get; set; }

        public string Email { get; set; } = null!;

        public string Username { get; set; } = null!;

        public string PhoneNumber { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public int FailedLoginCount { get; set; }

        public DateTimeOffset? LockoutEnd { get; set; }

        public bool IsEmailVerified { get; set; }

        public bool IsPhoneVerified { get; set; }

        public bool IsTwoFactorEnabled { get; set; }

        public EUserStatus Status { get; set; }

        // Navigation

        public ICollection<UserRole> UserRoles { get; set; } = [];

        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

        public ICollection<Device> Devices { get; set; } = [];

        public ICollection<UserSession> Sessions { get; set; } = [];

        public ICollection<LoginAttempt> LoginAttempts { get; set; } = [];

        public ICollection<AuditLog> AuditLogs { get; set; } = [];
    }
}
