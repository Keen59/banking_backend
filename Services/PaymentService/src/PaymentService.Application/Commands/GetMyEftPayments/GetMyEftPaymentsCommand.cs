using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.EftPayments;

namespace PaymentService.Application.Commands.GetMyEftPayments;

public class GetMyEftPaymentsCommand : IRequest<GetMyEftPaymentsResponse>
{
    public Guid CustomerId { get; set; }
}

public class GetMyEftPaymentsResponse : Response
{
    public IReadOnlyList<EftPaymentDto> Data { get; set; } = [];
}
