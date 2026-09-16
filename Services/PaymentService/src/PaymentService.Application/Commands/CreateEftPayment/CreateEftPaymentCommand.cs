using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.EftPayments;

namespace PaymentService.Application.Commands.CreateEftPayment;

public class CreateEftPaymentCommand : IRequest<CreateEftPaymentResponse>
{
    public Guid CustomerId { get; set; }

    public Guid SourceAccountId { get; set; }

    public string DestinationIban { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string Description { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;
}

public class CreateEftPaymentResponse : Response
{
    public EftPaymentDto? Data { get; set; }
}
