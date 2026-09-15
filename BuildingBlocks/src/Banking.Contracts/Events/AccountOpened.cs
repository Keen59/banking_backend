namespace Banking.Contracts.Events;

public sealed record AccountOpened(
    Guid AccountId,
    Guid CustomerId,
    string CifNumber,
    string Iban,
    string Currency,
    DateTimeOffset OccurredAt);
