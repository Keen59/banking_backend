using MediatR;
using LedgerService.Application.DTOs;

namespace LedgerService.Application.Commands.HoldEftPayment;

public class HoldEftPaymentCommand : IRequest<HoldEftPaymentResponse>
{
    public Guid EftPaymentId { get; init; }

    public Guid SourceAccountId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "TRY";
}

public class HoldEftPaymentResponse : Response
{
    public Guid? JournalEntryId { get; set; }
}
