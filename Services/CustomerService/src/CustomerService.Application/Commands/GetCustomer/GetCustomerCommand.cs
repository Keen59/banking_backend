using MediatR;

namespace CustomerService.Application.Commands.GetCustomer;

public class GetCustomerCommand : IRequest<GetCustomerResponse>
{
    public Guid CustomerId { get; init; }

    public Guid RequestedCustomerId { get; init; }

    public bool CanReadAny { get; init; }
}
