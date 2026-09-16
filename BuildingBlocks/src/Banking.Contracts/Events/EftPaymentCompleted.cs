namespace Banking.Contracts.Events;

public sealed record EftPaymentCompleted(
    Guid EftPaymentId,
    Guid JournalEntryId,
    DateTimeOffset OccurredAt);
