using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.IncomingEftPayments;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Mapping;

namespace PaymentService.Application.Commands.GetMyIncomingEftPayments;

public class GetMyIncomingEftPaymentsCommand : IRequest<GetMyIncomingEftPaymentsResponse>
{
    public Guid CustomerId { get; set; }
}

public class GetMyIncomingEftPaymentsResponse : Response
{
    public IReadOnlyList<IncomingEftPaymentDto> Data { get; set; } = [];
}

public class GetMyIncomingEftPaymentsHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMyIncomingEftPaymentsCommand, GetMyIncomingEftPaymentsResponse>
{
    public async Task<GetMyIncomingEftPaymentsResponse> Handle(
        GetMyIncomingEftPaymentsCommand request,
        CancellationToken cancellationToken)
    {
        var payments = await unitOfWork.IncomingEftPayments.GetByCustomerIdAsync(
            request.CustomerId,
            cancellationToken);

        return new GetMyIncomingEftPaymentsResponse
        {
            Message = "Incoming EFT payments listed.",
            Data = payments.Select(IncomingEftPaymentMapper.ToDto).ToList()
        };
    }
}
