namespace Banking.Contracts.Events;

public sealed record KycApproved(
    Guid CustomerId,
    string CifNumber,
    DateTimeOffset OccurredAt,
    string FirstName = "",
    string LastName = "",
    string? NationalId = null);
