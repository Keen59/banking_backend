namespace Banking.Contracts.Events;

public sealed record CardAuthorizationCompleted(
    Guid AuthorizationId,
    Guid JournalEntryId,
    DateTimeOffset OccurredAt);
