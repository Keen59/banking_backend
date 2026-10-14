namespace Banking.Contracts.Events;

public sealed record IncomingEftReturnRequested(
    Guid IncomingEftPaymentId,
    string Reason,
    DateTimeOffset OccurredAt);
