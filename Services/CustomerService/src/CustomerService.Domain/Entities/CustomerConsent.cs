using CustomerService.Domain.Enums;

namespace CustomerService.Domain.Entities;

public class CustomerConsent : BaseEntity
{
    public Guid CustomerId { get; set; }

    public EConsentType Type { get; set; }

    public bool Granted { get; set; }

    public DateTimeOffset GrantedAt { get; set; }

    public string? IpAddress { get; set; }

    public Customer Customer { get; set; } = null!;
}
