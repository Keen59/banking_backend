namespace Banking.Contracts.Events;

public sealed record OpenBankingPaymentRejected(
    Guid InitiationId,
    string Reason,
    DateTimeOffset OccurredAt);
