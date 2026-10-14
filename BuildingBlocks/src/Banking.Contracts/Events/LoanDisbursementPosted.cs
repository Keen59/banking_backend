namespace Banking.Contracts.Events;

public sealed record LoanDisbursementPosted(
    Guid LoanId,
    Guid JournalEntryId,
    DateTimeOffset OccurredAt);
