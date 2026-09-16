namespace Banking.Contracts.Events;

public sealed record EftSettlementRequested(
    Guid EftPaymentId,
    DateTimeOffset OccurredAt);
