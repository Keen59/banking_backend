namespace Banking.Contracts.Events;

public sealed record LoanDisbursementRequested(
    Guid LoanId,
    Guid AccountId,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredAt);
