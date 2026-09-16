namespace Banking.Contracts.Events;

public sealed record TestCreditPosted(
    Guid CreditId,
    Guid JournalEntryId,
    DateTimeOffset OccurredAt);
