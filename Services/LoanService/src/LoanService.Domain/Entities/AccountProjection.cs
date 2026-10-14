using LoanService.Domain.Enums;

namespace LoanService.Domain.Entities;

public class AccountProjection : BaseEntity
{
    public Guid CustomerId { get; set; }

    public string Currency { get; set; } = "TRY";

    public EAccountProjectionStatus Status { get; set; }
}
