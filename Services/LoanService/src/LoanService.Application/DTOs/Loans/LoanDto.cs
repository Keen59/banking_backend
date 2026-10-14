using LoanService.Domain.Enums;

namespace LoanService.Application.DTOs.Loans;

public class LoanDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Guid AccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public int TermMonths { get; set; }

    public decimal RemainingPrincipal { get; set; }

    public ELoanStatus Status { get; set; }

    public string? RejectReason { get; set; }

    public Guid? JournalEntryId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public IReadOnlyList<LoanInstallmentDto> Installments { get; set; } = [];
}
