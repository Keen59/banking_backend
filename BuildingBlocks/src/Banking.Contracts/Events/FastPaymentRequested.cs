namespace Banking.Contracts.Events;

public sealed record FastPaymentRequested(
    Guid FastPaymentId,
    Guid SourceAccountId,
    string DestinationIban,
    decimal Amount,
    string Currency,
    string Description,
    DateTimeOffset OccurredAt);
