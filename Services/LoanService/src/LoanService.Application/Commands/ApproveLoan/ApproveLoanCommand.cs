using MediatR;
using LoanService.Application.DTOs;
using LoanService.Application.DTOs.Loans;

namespace LoanService.Application.Commands.ApproveLoan;

public class ApproveLoanCommand : IRequest<ApproveLoanResponse>
{
    public Guid LoanId { get; set; }
}

public class ApproveLoanResponse : Response
{
    public LoanDto? Data { get; set; }
}
