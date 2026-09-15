namespace Banking.Contracts.Events;

public sealed record TransferCompleted(
    Guid TransferId,
    Guid JournalEntryId,
    DateTimeOffset OccurredAt);
