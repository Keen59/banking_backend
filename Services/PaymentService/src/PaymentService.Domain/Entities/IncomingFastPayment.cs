using PaymentService.Domain.Enums;

namespace PaymentService.Domain.Entities;

public class IncomingFastPayment : BaseEntity
{
    public Guid AccountId { get; set; }

    public Guid AccountCustomerId { get; set; }

    public Guid RequestedByCustomerId { get; set; }

    public string DestinationIban { get; set; } = null!;

    public string SourceIban { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string Description { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = null!;

    public ETransferStatus Status { get; set; }

    public string? RejectReason { get; set; }

    public Guid? JournalEntryId { get; set; }
}
