namespace Banking.Contracts.Events;

public sealed record TestCreditRejected(
    Guid CreditId,
    string Reason,
    DateTimeOffset OccurredAt);
