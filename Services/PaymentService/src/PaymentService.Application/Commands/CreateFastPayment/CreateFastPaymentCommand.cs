using MediatR;
using PaymentService.Application.DTOs;
using PaymentService.Application.DTOs.FastPayments;

namespace PaymentService.Application.Commands.CreateFastPayment;

public class CreateFastPaymentCommand : IRequest<CreateFastPaymentResponse>
{
    public Guid CustomerId { get; set; }

    public Guid SourceAccountId { get; set; }

    public string DestinationIban { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string Description { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;
}

public class CreateFastPaymentResponse : Response
{
    public FastPaymentDto? Data { get; set; }
}
