using PaymentService.Domain.Enums;

namespace PaymentService.Domain.Entities;

public class AccountProjection : BaseEntity
{
    public Guid CustomerId { get; set; }

    public string Iban { get; set; } = null!;

    public string Currency { get; set; } = "TRY";

    public EAccountProjectionStatus Status { get; set; }
}
