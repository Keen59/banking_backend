namespace Banking.Contracts.Events;

public sealed record LoanRepaymentRequested(
    Guid LoanId,
    Guid InstallmentId,
    Guid AccountId,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredAt);
