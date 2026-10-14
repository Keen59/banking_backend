namespace Banking.Contracts.Events;

public sealed record IncomingEftPaymentCompleted(
    Guid IncomingEftPaymentId,
    Guid JournalEntryId,
    DateTimeOffset OccurredAt);
