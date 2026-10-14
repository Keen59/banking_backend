namespace Banking.Contracts.Events;

public sealed record LoanDisbursementRejected(
    Guid LoanId,
    string Reason,
    DateTimeOffset OccurredAt);
