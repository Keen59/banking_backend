using MediatR;
using LedgerService.Application.DTOs;

namespace LedgerService.Application.Commands.ReturnIncomingEftPayment;

public class ReturnIncomingEftPaymentCommand : IRequest<ReturnIncomingEftPaymentResponse>
{
    public Guid IncomingEftPaymentId { get; init; }

    public string Reason { get; init; } = string.Empty;
}

public class ReturnIncomingEftPaymentResponse : Response;
