using MediatR;
using LedgerService.Application.DTOs;

namespace LedgerService.Application.Commands.ApplyIncomingFastPayment;

public class ApplyIncomingFastPaymentCommand : IRequest<ApplyIncomingFastPaymentResponse>
{
    public Guid IncomingFastPaymentId { get; init; }

    public Guid DestinationAccountId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "TRY";

    public string Description { get; init; } = string.Empty;
}

public class ApplyIncomingFastPaymentResponse : Response
{
    public Guid JournalEntryId { get; set; }
}
