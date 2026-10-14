using MediatR;
using OpenBankingService.Application.DTOs;
using OpenBankingService.Application.DTOs.Payments;

namespace OpenBankingService.Application.Commands.InitiatePayment;

public class InitiatePaymentCommand : IRequest<InitiatePaymentResponse>
{
    public string AccessToken { get; set; } = string.Empty;

    public Guid SourceAccountId { get; set; }

    public Guid DestinationAccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string Description { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;
}

public class InitiatePaymentResponse : Response
{
    public PaymentInitiationDto? Data { get; set; }
}
