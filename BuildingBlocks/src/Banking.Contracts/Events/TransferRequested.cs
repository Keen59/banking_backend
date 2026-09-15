namespace Banking.Contracts.Events;

public sealed record TransferRequested(
    Guid TransferId,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Currency,
    string Description,
    DateTimeOffset OccurredAt);
