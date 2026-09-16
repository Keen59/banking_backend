namespace Banking.Contracts.Events;

public sealed record EftReturnRequested(
    Guid EftPaymentId,
    string Reason,
    DateTimeOffset OccurredAt);
