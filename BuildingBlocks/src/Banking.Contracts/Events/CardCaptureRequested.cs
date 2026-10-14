namespace Banking.Contracts.Events;

public sealed record CardCaptureRequested(
    Guid AuthorizationId,
    string Description,
    DateTimeOffset OccurredAt);
