namespace Banking.Contracts.Events;

public sealed record FxConversionCompleted(
    Guid ConversionId,
    Guid SellJournalEntryId,
    Guid BuyJournalEntryId,
    DateTimeOffset OccurredAt);
