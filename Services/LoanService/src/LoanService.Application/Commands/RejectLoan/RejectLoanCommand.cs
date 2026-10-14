using MediatR;
using LoanService.Application.DTOs;
using LoanService.Application.DTOs.Loans;

namespace LoanService.Application.Commands.RejectLoan;

public class RejectLoanCommand : IRequest<RejectLoanResponse>
{
    public Guid LoanId { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public class RejectLoanResponse : Response
{
    public LoanDto? Data { get; set; }
}
