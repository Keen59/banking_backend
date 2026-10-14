using LoanService.Domain.Enums;

namespace LoanService.Domain.Entities;

public class LoanInstallment : BaseEntity
{
    public Guid LoanId { get; set; }

    public Loan Loan { get; set; } = null!;

    public int Sequence { get; set; }

    public DateTimeOffset DueDate { get; set; }

    public decimal Amount { get; set; }

    public EInstallmentStatus Status { get; set; }

    public DateTimeOffset? PaidAt { get; set; }

    public Guid? JournalEntryId { get; set; }
}
