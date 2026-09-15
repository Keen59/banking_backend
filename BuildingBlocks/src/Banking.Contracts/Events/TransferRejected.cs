namespace Banking.Contracts.Events;

public sealed record TransferRejected(
    Guid TransferId,
    string Reason,
    DateTimeOffset OccurredAt);
