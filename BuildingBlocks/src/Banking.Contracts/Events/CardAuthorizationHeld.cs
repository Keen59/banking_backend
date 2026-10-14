namespace Banking.Contracts.Events;

public sealed record CardAuthorizationHeld(
    Guid AuthorizationId,
    DateTimeOffset OccurredAt);
