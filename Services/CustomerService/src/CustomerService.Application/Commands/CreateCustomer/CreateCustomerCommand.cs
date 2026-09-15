using CustomerService.Domain.Enums;
using MediatR;

namespace CustomerService.Application.Commands.CreateCustomer;

public class CreateCustomerCommand : IRequest<CreateCustomerResponse>
{
    public Guid CustomerId { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string NationalId { get; init; } = string.Empty;

    public DateOnly DateOfBirth { get; init; }

    public string Nationality { get; init; } = "TR";

    public string Email { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string District { get; init; } = string.Empty;

    public string Line1 { get; init; } = string.Empty;

    public string? PostalCode { get; init; }

    public bool KvkkConsent { get; init; }

    public string? IpAddress { get; init; }
}
