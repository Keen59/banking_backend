using MediatR;
using LoanService.Application.DTOs;
using LoanService.Application.DTOs.Loans;
using LoanService.Application.Interfaces.Repositories;
using LoanService.Application.Mapping;

namespace LoanService.Application.Commands.GetLoan;

public class GetLoanCommand : IRequest<GetLoanResponse>
{
    public Guid LoanId { get; set; }

    public Guid CustomerId { get; set; }

    public bool CanReadAny { get; set; }
}

public class GetLoanResponse : Response
{
    public LoanDto? Data { get; set; }
}

public class GetLoanHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetLoanCommand, GetLoanResponse>
{
    public async Task<GetLoanResponse> Handle(
        GetLoanCommand request,
        CancellationToken cancellationToken)
    {
        var loan = await unitOfWork.Loans.GetByIdWithInstallmentsAsync(request.LoanId, cancellationToken);
        if (loan is null)
            throw new KeyNotFoundException("Loan not found.");

        if (!request.CanReadAny && loan.CustomerId != request.CustomerId)
            throw new KeyNotFoundException("Loan not found.");

        return new GetLoanResponse
        {
            Message = "Loan retrieved.",
            Data = LoanMapper.ToDto(loan)
        };
    }
}
