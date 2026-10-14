namespace Banking.Contracts.Events;

public sealed record CardAuthorizationRequested(
    Guid AuthorizationId,
    Guid AccountId,
    decimal Amount,
    string Currency,
    string Merchant,
    string Description,
    DateTimeOffset OccurredAt);
