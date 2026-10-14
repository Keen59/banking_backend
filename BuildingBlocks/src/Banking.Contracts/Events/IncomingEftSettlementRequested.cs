namespace Banking.Contracts.Events;

public sealed record IncomingEftSettlementRequested(
    Guid IncomingEftPaymentId,
    Guid DestinationAccountId,
    decimal Amount,
    string Currency,
    string Description,
    DateTimeOffset OccurredAt);
