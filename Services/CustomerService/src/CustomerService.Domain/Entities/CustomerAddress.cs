using CustomerService.Domain.Enums;

namespace CustomerService.Domain.Entities;

public class CustomerAddress : BaseEntity
{
    public Guid CustomerId { get; set; }

    public EAddressType Type { get; set; }

    public string Country { get; set; } = "TR";

    public string City { get; set; } = null!;

    public string District { get; set; } = null!;

    public string Line1 { get; set; } = null!;

    public string? PostalCode { get; set; }

    public bool IsPrimary { get; set; }

    public Customer Customer { get; set; } = null!;
}
