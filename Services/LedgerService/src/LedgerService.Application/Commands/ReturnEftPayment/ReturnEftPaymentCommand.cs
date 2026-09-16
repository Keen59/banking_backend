using MediatR;
using LedgerService.Application.DTOs;

namespace LedgerService.Application.Commands.ReturnEftPayment;

public class ReturnEftPaymentCommand : IRequest<ReturnEftPaymentResponse>
{
    public Guid EftPaymentId { get; init; }

    public string Reason { get; init; } = string.Empty;
}

public class ReturnEftPaymentResponse : Response;
