using MediatR;
using LedgerService.Application.DTOs;

namespace LedgerService.Application.Commands.AcceptIncomingEftPayment;

public class AcceptIncomingEftPaymentCommand : IRequest<AcceptIncomingEftPaymentResponse>
{
    public Guid IncomingEftPaymentId { get; init; }

    public Guid DestinationAccountId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "TRY";
}

public class AcceptIncomingEftPaymentResponse : Response
{
    public Guid? JournalEntryId { get; set; }
}
