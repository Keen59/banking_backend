using MediatR;
using LoanService.Application.DTOs;
using LoanService.Application.DTOs.Loans;
using LoanService.Application.Interfaces.Repositories;
using LoanService.Application.Mapping;

namespace LoanService.Application.Commands.GetMyLoans;

public class GetMyLoansCommand : IRequest<GetMyLoansResponse>
{
    public Guid CustomerId { get; set; }
}

public class GetMyLoansResponse : Response
{
    public IReadOnlyList<LoanDto> Data { get; set; } = [];
}

public class GetMyLoansHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMyLoansCommand, GetMyLoansResponse>
{
    public async Task<GetMyLoansResponse> Handle(
        GetMyLoansCommand request,
        CancellationToken cancellationToken)
    {
        var loans = await unitOfWork.Loans.GetByCustomerIdAsync(request.CustomerId, cancellationToken);

        return new GetMyLoansResponse
        {
            Message = "Loans listed.",
            Data = loans.Select(LoanMapper.ToDto).ToList()
        };
    }
}
