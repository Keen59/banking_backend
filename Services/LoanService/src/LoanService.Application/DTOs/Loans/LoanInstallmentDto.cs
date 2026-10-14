using LoanService.Domain.Enums;

namespace LoanService.Application.DTOs.Loans;

public class LoanInstallmentDto
{
    public Guid Id { get; set; }

    public int Sequence { get; set; }

    public DateTimeOffset DueDate { get; set; }

    public decimal Amount { get; set; }

    public EInstallmentStatus Status { get; set; }

    public DateTimeOffset? PaidAt { get; set; }

    public Guid? JournalEntryId { get; set; }
}
