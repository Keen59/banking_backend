namespace Banking.Contracts.Events;

public sealed record NotificationRequested(
    Guid CustomerId,
    string Type,
    string Title,
    string Body,
    Guid SourceId,
    DateTimeOffset OccurredAt);
