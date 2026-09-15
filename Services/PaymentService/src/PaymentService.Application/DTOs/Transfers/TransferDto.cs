using PaymentService.Domain.Enums;

namespace PaymentService.Application.DTOs.Transfers;

public class TransferDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Guid SourceAccountId { get; set; }

    public Guid DestinationAccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ETransferStatus Status { get; set; }

    public string? RejectReason { get; set; }

    public Guid? JournalEntryId { get; set; }
}
