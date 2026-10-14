namespace Banking.Contracts.Events;

public sealed record LoanRepaymentRejected(
    Guid LoanId,
    Guid InstallmentId,
    string Reason,
    DateTimeOffset OccurredAt);
