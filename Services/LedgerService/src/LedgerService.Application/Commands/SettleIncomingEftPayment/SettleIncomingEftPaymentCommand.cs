using MediatR;
using LedgerService.Application.DTOs;

namespace LedgerService.Application.Commands.SettleIncomingEftPayment;

public class SettleIncomingEftPaymentCommand : IRequest<SettleIncomingEftPaymentResponse>
{
    public Guid IncomingEftPaymentId { get; init; }

    public Guid DestinationAccountId { get; init; }

    public string Description { get; init; } = string.Empty;
}

public class SettleIncomingEftPaymentResponse : Response
{
    public Guid JournalEntryId { get; set; }
}
