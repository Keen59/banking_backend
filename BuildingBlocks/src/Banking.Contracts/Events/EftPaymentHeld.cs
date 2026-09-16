namespace Banking.Contracts.Events;

public sealed record EftPaymentHeld(
    Guid EftPaymentId,
    DateTimeOffset OccurredAt);
