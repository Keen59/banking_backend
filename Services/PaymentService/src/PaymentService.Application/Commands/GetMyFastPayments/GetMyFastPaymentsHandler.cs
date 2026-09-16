using MediatR;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Mapping;

namespace PaymentService.Application.Commands.GetMyFastPayments;

public class GetMyFastPaymentsHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMyFastPaymentsCommand, GetMyFastPaymentsResponse>
{
    public async Task<GetMyFastPaymentsResponse> Handle(
        GetMyFastPaymentsCommand request,
        CancellationToken cancellationToken)
    {
        var payments = await unitOfWork.FastPayments.GetByCustomerIdAsync(request.CustomerId, cancellationToken);

        return new GetMyFastPaymentsResponse
        {
            Message = "FAST payments listed.",
            Data = payments.Select(FastPaymentMapper.ToDto).ToList()
        };
    }
}
