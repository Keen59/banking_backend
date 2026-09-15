namespace Banking.Contracts.Events;

public sealed record UserRegistered(
    Guid UserId,
    Guid CustomerId,
    string Email,
    string Username,
    string PhoneNumber,
    DateTimeOffset OccurredAt);
