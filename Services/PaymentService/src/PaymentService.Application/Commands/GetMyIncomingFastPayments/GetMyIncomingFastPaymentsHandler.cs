using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.IncomingFastPayments;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Mapping;

namespace PaymentService.Application.Commands.GetMyIncomingFastPayments;

public class GetMyIncomingFastPaymentsCommand : IRequest<GetMyIncomingFastPaymentsResponse>
{
    public Guid CustomerId { get; set; }
}

public class GetMyIncomingFastPaymentsResponse : Response
{
    public IReadOnlyList<IncomingFastPaymentDto> Data { get; set; } = [];
}

public class GetMyIncomingFastPaymentsHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMyIncomingFastPaymentsCommand, GetMyIncomingFastPaymentsResponse>
{
    public async Task<GetMyIncomingFastPaymentsResponse> Handle(
        GetMyIncomingFastPaymentsCommand request,
        CancellationToken cancellationToken)
    {
        var payments = await unitOfWork.IncomingFastPayments.GetByCustomerIdAsync(
            request.CustomerId,
            cancellationToken);

        return new GetMyIncomingFastPaymentsResponse
        {
            Message = "Incoming FAST payments listed.",
            Data = payments.Select(IncomingFastPaymentMapper.ToDto).ToList()
        };
    }
}
