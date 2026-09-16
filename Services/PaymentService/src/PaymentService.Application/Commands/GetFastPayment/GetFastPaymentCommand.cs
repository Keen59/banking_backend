using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.FastPayments;

namespace PaymentService.Application.Commands.GetFastPayment;

public class GetFastPaymentCommand : IRequest<GetFastPaymentResponse>
{
    public Guid FastPaymentId { get; set; }

    public Guid CustomerId { get; set; }

    public bool CanReadAny { get; set; }
}

public class GetFastPaymentResponse : Response
{
    public FastPaymentDto? Data { get; set; }
}
