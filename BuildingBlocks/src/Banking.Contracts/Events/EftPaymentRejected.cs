namespace Banking.Contracts.Events;

public sealed record EftPaymentRejected(
    Guid EftPaymentId,
    string Reason,
    DateTimeOffset OccurredAt);
