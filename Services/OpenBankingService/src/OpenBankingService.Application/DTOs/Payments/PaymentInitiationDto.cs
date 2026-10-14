using OpenBankingService.Domain.Enums;

namespace OpenBankingService.Application.DTOs.Payments;

public class PaymentInitiationDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Guid ConsentId { get; set; }

    public Guid SourceAccountId { get; set; }

    public Guid DestinationAccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public EInitiationStatus Status { get; set; }

    public string? RejectReason { get; set; }

    public Guid? JournalEntryId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
