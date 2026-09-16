using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.FastPayments;

namespace PaymentService.Application.Commands.GetMyFastPayments;

public class GetMyFastPaymentsCommand : IRequest<GetMyFastPaymentsResponse>
{
    public Guid CustomerId { get; set; }
}

public class GetMyFastPaymentsResponse : Response
{
    public IReadOnlyList<FastPaymentDto> Data { get; set; } = [];
}
