using AuthService.Domain.Enums;

namespace AuthService.Domain.Entities;

public class EmailOtp : BaseEntity
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = null!;

    public EOtpPurpose Purpose { get; set; }

    public string CodeHash { get; set; } = null!;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ConsumedAt { get; set; }

    public int FailedAttemptCount { get; set; }

    public User User { get; set; } = null!;
}
