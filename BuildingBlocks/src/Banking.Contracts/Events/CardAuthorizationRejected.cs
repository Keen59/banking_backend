namespace Banking.Contracts.Events;

public sealed record CardAuthorizationRejected(
    Guid AuthorizationId,
    string Reason,
    DateTimeOffset OccurredAt);
