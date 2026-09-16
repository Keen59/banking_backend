namespace Banking.Contracts.Events;

public sealed record IncomingFastPaymentRejected(
    Guid IncomingFastPaymentId,
    string Reason,
    DateTimeOffset OccurredAt);
