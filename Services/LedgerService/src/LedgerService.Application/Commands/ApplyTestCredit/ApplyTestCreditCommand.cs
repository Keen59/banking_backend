using MediatR;
using LedgerService.Application.DTOs;

namespace LedgerService.Application.Commands.ApplyTestCredit;

public class ApplyTestCreditCommand : IRequest<ApplyTestCreditResponse>
{
    public Guid CreditId { get; init; }

    public Guid AccountId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "TRY";
}

public class ApplyTestCreditResponse : Response
{
    public Guid JournalEntryId { get; set; }
}
