using PaymentService.Domain.Enums;

namespace PaymentService.Application.DTOs.TestCredits;

public class TestCreditDto
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public Guid AccountCustomerId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public ETransferStatus Status { get; set; }

    public string? RejectReason { get; set; }

    public Guid? JournalEntryId { get; set; }
}
