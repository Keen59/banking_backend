namespace Banking.Contracts.Events;

public sealed record LoanRepaymentPosted(
    Guid LoanId,
    Guid InstallmentId,
    Guid JournalEntryId,
    DateTimeOffset OccurredAt);
