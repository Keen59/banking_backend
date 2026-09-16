using MediatR;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Mapping;

namespace PaymentService.Application.Commands.GetFastPayment;

public class GetFastPaymentHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetFastPaymentCommand, GetFastPaymentResponse>
{
    public async Task<GetFastPaymentResponse> Handle(
        GetFastPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.FastPayments.GetByIdAsync(request.FastPaymentId);
        if (payment is null)
            throw new KeyNotFoundException("FAST payment not found.");

        if (!request.CanReadAny && payment.CustomerId != request.CustomerId)
            throw new KeyNotFoundException("FAST payment not found.");

        return new GetFastPaymentResponse
        {
            Message = "FAST payment retrieved.",
            Data = FastPaymentMapper.ToDto(payment)
        };
    }
}
