using MediatR;

namespace CustomerService.Application.Commands.GetPendingKyc;

public class GetPendingKycCommand : IRequest<GetPendingKycResponse>
{
}
