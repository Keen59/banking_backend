using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.IncomingEftPayments;

namespace PaymentService.Application.Commands.CreateIncomingEftPayment;

public class CreateIncomingEftPaymentCommand : IRequest<CreateIncomingEftPaymentResponse>
{
    public Guid RequestedByCustomerId { get; set; }

    public string DestinationIban { get; set; } = string.Empty;

    public string SourceIban { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string Description { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;
}

public class CreateIncomingEftPaymentResponse : Response
{
    public IncomingEftPaymentDto? Data { get; set; }
}
