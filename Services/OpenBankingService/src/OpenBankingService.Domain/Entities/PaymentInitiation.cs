using OpenBankingService.Domain.Enums;

namespace OpenBankingService.Domain.Entities;

public class PaymentInitiation : BaseEntity
{
    public Guid CustomerId { get; set; }

    public Guid ConsentId { get; set; }

    public Guid SourceAccountId { get; set; }

    public Guid DestinationAccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string Description { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = null!;

    public EInitiationStatus Status { get; set; }

    public string? RejectReason { get; set; }

    public Guid? JournalEntryId { get; set; }
}
