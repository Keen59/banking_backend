using PaymentService.Domain.Enums;

namespace PaymentService.Application.DTOs.FxConversions;

public class FxConversionDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Guid SourceAccountId { get; set; }

    public Guid DestinationAccountId { get; set; }

    public decimal SourceAmount { get; set; }

    public string SourceCurrency { get; set; } = string.Empty;

    public decimal DestinationAmount { get; set; }

    public string DestinationCurrency { get; set; } = string.Empty;

    public decimal Rate { get; set; }

    public decimal TryEquivalent { get; set; }

    public string Description { get; set; } = string.Empty;

    public ETransferStatus Status { get; set; }

    public string? RejectReason { get; set; }

    public Guid? SellJournalEntryId { get; set; }

    public Guid? BuyJournalEntryId { get; set; }
}
