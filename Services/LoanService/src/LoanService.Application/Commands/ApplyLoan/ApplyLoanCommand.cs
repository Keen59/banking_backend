using MediatR;
using LoanService.Application.DTOs;
using LoanService.Application.DTOs.Loans;

namespace LoanService.Application.Commands.ApplyLoan;

public class ApplyLoanCommand : IRequest<ApplyLoanResponse>
{
    public Guid CustomerId { get; set; }

    public Guid AccountId { get; set; }

    public decimal Amount { get; set; }

    public int TermMonths { get; set; }

    public string Currency { get; set; } = "TRY";

    public string IdempotencyKey { get; set; } = string.Empty;
}

public class ApplyLoanResponse : Response
{
    public LoanDto? Data { get; set; }
}
