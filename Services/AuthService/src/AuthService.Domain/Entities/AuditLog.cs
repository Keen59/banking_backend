using AuthService.Domain.Enums;

namespace AuthService.Domain.Entities
{
    public class AuditLog : BaseEntity
    {
        public Guid? UserId { get; set; }

        public EAuditAction Action { get; set; }

        public string Resource { get; set; } = null!;

        public string IpAddress { get; set; } = null!;

        public string Device { get; set; } = null!;

        public string Metadata { get; set; } = "{}";

        public User? User { get; set; }
    }
}
