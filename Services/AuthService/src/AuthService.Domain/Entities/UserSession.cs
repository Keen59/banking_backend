namespace AuthService.Domain.Entities
{
    public class UserSession : BaseEntity
    {
        public Guid UserId { get; set; }

        public Guid RefreshTokenId { get; set; }

        public Guid JwtId { get; set; }

        public Guid? DeviceId { get; set; }

        public string IpAddress { get; set; } = null!;

        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset RevokedAt { get; set; }

        public DateTimeOffset LastActivityAt { get; set; }

        public bool IsActive { get; set; }

        public User User { get; set; } = null!;

        public RefreshToken RefreshToken { get; set; } = null!;
    }
}
