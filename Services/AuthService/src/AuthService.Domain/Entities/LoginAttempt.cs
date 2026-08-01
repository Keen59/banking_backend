using AuthService.Domain.Enums;

namespace AuthService.Domain.Entities
{
    public class LoginAttempt : BaseEntity
    {
        public Guid? UserId { get; set; }

        public string Email { get; set; } = null!;

        public string IpAddress { get; set; } = null!;

        public string Device { get; set; } = null!;

        public bool IsSuccessful { get; set; }

        public ELoginFailureReason FailureReason { get; set; }

        public User? User { get; set; }
    }
}
