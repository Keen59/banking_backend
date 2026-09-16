using PaymentService.Domain.Enums;

namespace PaymentService.Domain.Entities;

public class TestCredit : BaseEntity
{
    public Guid AccountId { get; set; }

    public Guid AccountCustomerId { get; set; }

    public Guid RequestedByCustomerId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string IdempotencyKey { get; set; } = null!;

    public ETransferStatus Status { get; set; }

    public string? RejectReason { get; set; }

    public Guid? JournalEntryId { get; set; }
}
