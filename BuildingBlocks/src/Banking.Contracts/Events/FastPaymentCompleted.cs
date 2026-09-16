namespace Banking.Contracts.Events;

public sealed record FastPaymentCompleted(
    Guid FastPaymentId,
    Guid JournalEntryId,
    DateTimeOffset OccurredAt);
