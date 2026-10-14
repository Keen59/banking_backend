using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.IncomingEftPayments;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Mapping;

namespace PaymentService.Application.Commands.GetIncomingEftPayment;

public class GetIncomingEftPaymentCommand : IRequest<GetIncomingEftPaymentResponse>
{
    public Guid IncomingEftPaymentId { get; set; }

    public Guid CustomerId { get; set; }

    public bool CanReadAny { get; set; }
}

public class GetIncomingEftPaymentResponse : Response
{
    public IncomingEftPaymentDto? Data { get; set; }
}

public class GetIncomingEftPaymentHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetIncomingEftPaymentCommand, GetIncomingEftPaymentResponse>
{
    public async Task<GetIncomingEftPaymentResponse> Handle(
        GetIncomingEftPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.IncomingEftPayments.GetByIdAsync(request.IncomingEftPaymentId);
        if (payment is null)
            throw new KeyNotFoundException("Incoming EFT payment not found.");

        if (!request.CanReadAny && payment.AccountCustomerId != request.CustomerId)
            throw new KeyNotFoundException("Incoming EFT payment not found.");

        return new GetIncomingEftPaymentResponse
        {
            Message = "Incoming EFT payment retrieved.",
            Data = IncomingEftPaymentMapper.ToDto(payment)
        };
    }
}
