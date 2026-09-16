namespace Banking.Contracts.Events;

public sealed record IncomingFastPaymentCompleted(
    Guid IncomingFastPaymentId,
    Guid JournalEntryId,
    DateTimeOffset OccurredAt);
