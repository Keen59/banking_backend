using MediatR;
using LedgerService.Application.DTOs;

namespace LedgerService.Application.Commands.ApplyLoanRepayment;

public class ApplyLoanRepaymentCommand : IRequest<ApplyLoanRepaymentResponse>
{
    public Guid LoanId { get; init; }

    public Guid InstallmentId { get; init; }

    public Guid AccountId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "TRY";
}

public class ApplyLoanRepaymentResponse : Response
{
    public Guid JournalEntryId { get; set; }
}
