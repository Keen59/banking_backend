namespace Banking.Contracts.Events;

public sealed record TestCreditRequested(
    Guid CreditId,
    Guid AccountId,
    decimal Amount,
    string Currency,
    DateTimeOffset OccurredAt);
