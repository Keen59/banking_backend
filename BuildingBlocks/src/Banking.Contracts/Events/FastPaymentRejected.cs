namespace Banking.Contracts.Events;

public sealed record FastPaymentRejected(
    Guid FastPaymentId,
    string Reason,
    DateTimeOffset OccurredAt);
