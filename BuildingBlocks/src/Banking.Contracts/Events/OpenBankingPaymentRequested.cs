namespace Banking.Contracts.Events;

public sealed record OpenBankingPaymentRequested(
    Guid InitiationId,
    Guid CustomerId,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string Currency,
    string Description,
    DateTimeOffset OccurredAt);
