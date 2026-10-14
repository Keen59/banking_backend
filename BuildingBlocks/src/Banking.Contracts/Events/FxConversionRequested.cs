namespace Banking.Contracts.Events;

public sealed record FxConversionRequested(
    Guid ConversionId,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal SourceAmount,
    string SourceCurrency,
    decimal DestinationAmount,
    string DestinationCurrency,
    decimal Rate,
    decimal TryEquivalent,
    DateTimeOffset OccurredAt);
