using MediatR;

namespace CustomerService.Application.Commands.SubmitKyc;

public class SubmitKycCommand : IRequest<SubmitKycResponse>
{
    public Guid CustomerId { get; init; }

    public Guid RequestedCustomerId { get; init; }

    public string? IpAddress { get; init; }
}
