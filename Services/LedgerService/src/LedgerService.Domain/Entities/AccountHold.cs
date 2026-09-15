using LedgerService.Domain.Enums;

namespace LedgerService.Domain.Entities;

public class AccountHold : BaseEntity
{
    public Guid LedgerAccountId { get; set; }

    public string IdempotencyKey { get; set; } = null!;

    public decimal Amount { get; set; }

    public EHoldStatus Status { get; set; }

    public LedgerAccount LedgerAccount { get; set; } = null!;
}
