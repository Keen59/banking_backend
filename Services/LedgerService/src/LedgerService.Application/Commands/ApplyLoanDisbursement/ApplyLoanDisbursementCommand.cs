using MediatR;
using LedgerService.Application.DTOs;

namespace LedgerService.Application.Commands.ApplyLoanDisbursement;

public class ApplyLoanDisbursementCommand : IRequest<ApplyLoanDisbursementResponse>
{
    public Guid LoanId { get; init; }

    public Guid AccountId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "TRY";
}

public class ApplyLoanDisbursementResponse : Response
{
    public Guid JournalEntryId { get; set; }
}
