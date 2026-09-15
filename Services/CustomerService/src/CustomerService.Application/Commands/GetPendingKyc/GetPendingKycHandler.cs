using CustomerService.Application.Interfaces.Repositories;
using CustomerService.Application.Mapping;
using MediatR;

namespace CustomerService.Application.Commands.GetPendingKyc;

public class GetPendingKycHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetPendingKycCommand, GetPendingKycResponse>
{
    public async Task<GetPendingKycResponse> Handle(GetPendingKycCommand request, CancellationToken cancellationToken)
    {
        var customers = await unitOfWork.CustomerRepository.ListPendingKycAsync(cancellationToken);

        return new GetPendingKycResponse
        {
            Message = "İnceleme bekleyen KYC kayıtları.",
            Customers = customers.Select(CustomerMapper.ToDto).ToList()
        };
    }
}
