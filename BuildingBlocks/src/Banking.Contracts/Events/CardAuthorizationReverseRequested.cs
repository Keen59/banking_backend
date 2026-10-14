namespace Banking.Contracts.Events;

public sealed record CardAuthorizationReverseRequested(
    Guid AuthorizationId,
    string Reason,
    DateTimeOffset OccurredAt);
