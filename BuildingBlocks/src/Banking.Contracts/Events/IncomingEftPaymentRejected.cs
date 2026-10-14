namespace Banking.Contracts.Events;

public sealed record IncomingEftPaymentRejected(
    Guid IncomingEftPaymentId,
    string Reason,
    DateTimeOffset OccurredAt);
