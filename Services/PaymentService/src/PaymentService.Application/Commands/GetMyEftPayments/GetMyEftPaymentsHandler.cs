using MediatR;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Mapping;

namespace PaymentService.Application.Commands.GetMyEftPayments;

public class GetMyEftPaymentsHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMyEftPaymentsCommand, GetMyEftPaymentsResponse>
{
    public async Task<GetMyEftPaymentsResponse> Handle(
        GetMyEftPaymentsCommand request,
        CancellationToken cancellationToken)
    {
        var payments = await unitOfWork.EftPayments.GetByCustomerIdAsync(request.CustomerId, cancellationToken);

        return new GetMyEftPaymentsResponse
        {
            Message = "EFT payments listed.",
            Data = payments.Select(EftPaymentMapper.ToDto).ToList()
        };
    }
}
