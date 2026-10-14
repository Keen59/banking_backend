using MediatR;
using LoanService.Application.DTOs;
using LoanService.Application.DTOs.Loans;

namespace LoanService.Application.Commands.RequestRepayment;

public class RequestRepaymentCommand : IRequest<RequestRepaymentResponse>
{
    public Guid CustomerId { get; set; }

    public Guid LoanId { get; set; }
}

public class RequestRepaymentResponse : Response
{
    public LoanDto? Data { get; set; }
}
