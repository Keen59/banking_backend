namespace Banking.Contracts.Events;

public sealed record IncomingEftPaymentRequested(
    Guid IncomingEftPaymentId,
    Guid DestinationAccountId,
    string DestinationIban,
    decimal Amount,
    string Currency,
    string Description,
    DateTimeOffset OccurredAt);
