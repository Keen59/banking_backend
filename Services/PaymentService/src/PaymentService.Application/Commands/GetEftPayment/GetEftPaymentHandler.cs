using MediatR;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Mapping;

namespace PaymentService.Application.Commands.GetEftPayment;

public class GetEftPaymentHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetEftPaymentCommand, GetEftPaymentResponse>
{
    public async Task<GetEftPaymentResponse> Handle(
        GetEftPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.EftPayments.GetByIdAsync(request.EftPaymentId);
        if (payment is null)
            throw new KeyNotFoundException("EFT payment not found.");

        if (!request.CanReadAny && payment.CustomerId != request.CustomerId)
            throw new KeyNotFoundException("EFT payment not found.");

        return new GetEftPaymentResponse
        {
            Message = "EFT payment retrieved.",
            Data = EftPaymentMapper.ToDto(payment)
        };
    }
}
