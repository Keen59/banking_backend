using MediatR;
using LedgerService.Application.DTOs;

namespace LedgerService.Application.Commands.SettleEftPayment;

public class SettleEftPaymentCommand : IRequest<SettleEftPaymentResponse>
{
    public Guid EftPaymentId { get; init; }

    public string Description { get; init; } = string.Empty;
}

public class SettleEftPaymentResponse : Response
{
    public Guid JournalEntryId { get; set; }
}
