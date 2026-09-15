using MediatR;

namespace CustomerService.Application.Commands.UpdateCustomer;

public class UpdateCustomerCommand : IRequest<UpdateCustomerResponse>
{
    public Guid CustomerId { get; init; }

    public Guid RequestedCustomerId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public string? City { get; init; }

    public string? District { get; init; }

    public string? Line1 { get; init; }

    public string? PostalCode { get; init; }

    public string? IpAddress { get; init; }
}
