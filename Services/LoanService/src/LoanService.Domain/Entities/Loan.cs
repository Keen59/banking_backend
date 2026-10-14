using LoanService.Domain.Enums;

namespace LoanService.Domain.Entities;

public class Loan : BaseEntity
{
    public Guid CustomerId { get; set; }

    public Guid AccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public int TermMonths { get; set; }

    public decimal RemainingPrincipal { get; set; }

    public string IdempotencyKey { get; set; } = null!;

    public ELoanStatus Status { get; set; }

    public string? RejectReason { get; set; }

    public Guid? JournalEntryId { get; set; }

    public ICollection<LoanInstallment> Installments { get; set; } = new List<LoanInstallment>();
}
