using MediatR;

namespace CustomerService.Application.Commands.ReviewKyc;

public class ReviewKycCommand : IRequest<ReviewKycResponse>
{
    public Guid CustomerId { get; init; }

    public bool Approved { get; init; }

    public string? RejectReason { get; init; }

    public string? IpAddress { get; init; }
}
