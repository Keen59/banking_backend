using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.IncomingFastPayments;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Mapping;

namespace PaymentService.Application.Commands.GetIncomingFastPayment;

public class GetIncomingFastPaymentCommand : IRequest<GetIncomingFastPaymentResponse>
{
    public Guid IncomingFastPaymentId { get; set; }

    public Guid CustomerId { get; set; }

    public bool CanReadAny { get; set; }
}

public class GetIncomingFastPaymentResponse : Response
{
    public IncomingFastPaymentDto? Data { get; set; }
}

public class GetIncomingFastPaymentHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetIncomingFastPaymentCommand, GetIncomingFastPaymentResponse>
{
    public async Task<GetIncomingFastPaymentResponse> Handle(
        GetIncomingFastPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.IncomingFastPayments.GetByIdAsync(request.IncomingFastPaymentId);
        if (payment is null)
            throw new KeyNotFoundException("Incoming FAST payment not found.");

        if (!request.CanReadAny && payment.AccountCustomerId != request.CustomerId)
            throw new KeyNotFoundException("Incoming FAST payment not found.");

        return new GetIncomingFastPaymentResponse
        {
            Message = "Incoming FAST payment retrieved.",
            Data = IncomingFastPaymentMapper.ToDto(payment)
        };
    }
}
