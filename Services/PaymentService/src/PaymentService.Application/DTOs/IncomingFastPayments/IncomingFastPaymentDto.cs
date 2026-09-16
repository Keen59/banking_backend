using PaymentService.Domain.Enums;

namespace PaymentService.Application.DTOs.IncomingFastPayments;

public class IncomingFastPaymentDto
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public Guid AccountCustomerId { get; set; }

    public string DestinationIban { get; set; } = string.Empty;

    public string SourceIban { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ETransferStatus Status { get; set; }

    public string? RejectReason { get; set; }

    public Guid? JournalEntryId { get; set; }
}
