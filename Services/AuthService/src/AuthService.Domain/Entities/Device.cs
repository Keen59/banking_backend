namespace AuthService.Domain.Entities
{
    public class Device : BaseEntity
    {
        public Guid UserId { get; set; }

        public string DeviceIdentifier { get; set; } = null!;

        public string DeviceName { get; set; } = null!;

        public string OperatingSystem { get; set; } = null!;

        public string Browser { get; set; } = null!;

        public string IpAddress { get; set; } = null!;

        public DateTimeOffset? LastLoginAt { get; set; }

        public bool IsTrusted { get; set; }

        public User User { get; set; } = null!;
    }
}
