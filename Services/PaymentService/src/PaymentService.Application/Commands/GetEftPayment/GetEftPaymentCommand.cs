using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.EftPayments;

namespace PaymentService.Application.Commands.GetEftPayment;

public class GetEftPaymentCommand : IRequest<GetEftPaymentResponse>
{
    public Guid EftPaymentId { get; set; }

    public Guid CustomerId { get; set; }

    public bool CanReadAny { get; set; }
}

public class GetEftPaymentResponse : Response
{
    public EftPaymentDto? Data { get; set; }
}
