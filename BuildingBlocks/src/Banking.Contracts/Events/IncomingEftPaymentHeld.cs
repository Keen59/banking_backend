namespace Banking.Contracts.Events;

public sealed record IncomingEftPaymentHeld(
    Guid IncomingEftPaymentId,
    DateTimeOffset OccurredAt);
