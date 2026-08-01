namespace AuthService.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public Guid UserId { get; set; }

        public string Token { get; set; } = null!;

        public DateTimeOffset ExpiresAt { get; set; }

        public DateTimeOffset? RevokedAt { get; set; }

        public string CreatedIp { get; set; } = null!;

        public string? RevokedIp { get; set; }

        public Guid? DeviceId { get; set; }

        public bool IsRevoked => RevokedAt.HasValue;

        public User User { get; set; } = null!;
    }
}
