using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.IncomingFastPayments;

namespace PaymentService.Application.Commands.CreateIncomingFastPayment;

public class CreateIncomingFastPaymentCommand : IRequest<CreateIncomingFastPaymentResponse>
{
    public Guid RequestedByCustomerId { get; set; }

    public string DestinationIban { get; set; } = string.Empty;

    public string SourceIban { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string Description { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;
}

public class CreateIncomingFastPaymentResponse : Response
{
    public IncomingFastPaymentDto? Data { get; set; }
}
