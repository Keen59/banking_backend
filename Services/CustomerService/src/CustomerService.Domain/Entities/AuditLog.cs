using CustomerService.Domain.Enums;

namespace CustomerService.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? CustomerId { get; set; }

    public EAuditAction Action { get; set; }

    public string Resource { get; set; } = null!;

    public string? IpAddress { get; set; }

    public string Metadata { get; set; } = "{}";

    public Customer? Customer { get; set; }
}
