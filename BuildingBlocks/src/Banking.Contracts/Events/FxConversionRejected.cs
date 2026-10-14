namespace Banking.Contracts.Events;

public sealed record FxConversionRejected(
    Guid ConversionId,
    string Reason,
    DateTimeOffset OccurredAt);
