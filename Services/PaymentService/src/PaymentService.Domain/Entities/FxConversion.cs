using PaymentService.Domain.Enums;

namespace PaymentService.Domain.Entities;

public class FxConversion : BaseEntity
{
    public Guid CustomerId { get; set; }

    public Guid SourceAccountId { get; set; }

    public Guid DestinationAccountId { get; set; }

    public decimal SourceAmount { get; set; }

    public string SourceCurrency { get; set; } = "TRY";

    public decimal DestinationAmount { get; set; }

    public string DestinationCurrency { get; set; } = "USD";

    public decimal Rate { get; set; }

    public decimal TryEquivalent { get; set; }

    public string Description { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = null!;

    public ETransferStatus Status { get; set; }

    public string? RejectReason { get; set; }

    public Guid? SellJournalEntryId { get; set; }

    public Guid? BuyJournalEntryId { get; set; }
}
