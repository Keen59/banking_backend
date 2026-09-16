namespace Banking.Contracts.Events;

public sealed record IncomingFastPaymentRequested(
    Guid IncomingFastPaymentId,
    Guid DestinationAccountId,
    string DestinationIban,
    decimal Amount,
    string Currency,
    string Description,
    DateTimeOffset OccurredAt);
