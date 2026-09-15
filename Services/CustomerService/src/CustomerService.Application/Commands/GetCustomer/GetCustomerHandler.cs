using CustomerService.Application.Interfaces.Repositories;
using CustomerService.Application.Mapping;
using MediatR;

namespace CustomerService.Application.Commands.GetCustomer;

public class GetCustomerHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCustomerCommand, GetCustomerResponse>
{
    public async Task<GetCustomerResponse> Handle(GetCustomerCommand request, CancellationToken cancellationToken)
    {
        if (!request.CanReadAny && request.RequestedCustomerId != request.CustomerId)
            throw new UnauthorizedAccessException("Bu müşteri kaydına erişim yetkiniz yok.");

        var customer = await unitOfWork.CustomerRepository.GetByIdWithDetailsAsync(
            request.CustomerId,
            asNoTracking: true,
            cancellationToken: cancellationToken) ?? throw new InvalidOperationException("Müşteri bulunamadı.");

        return new GetCustomerResponse
        {
            Message = "Müşteri kaydı.",
            Customer = CustomerMapper.ToDto(customer)
        };
    }
}
