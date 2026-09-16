namespace Banking.Contracts.Events;

public sealed record EftPaymentRequested(
    Guid EftPaymentId,
    Guid SourceAccountId,
    string DestinationIban,
    decimal Amount,
    string Currency,
    string Description,
    DateTimeOffset OccurredAt);
