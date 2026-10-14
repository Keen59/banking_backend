using Banking.Contracts.Events;
using MediatR;
using LoanService.Application.Interfaces.Repositories;
using LoanService.Application.Interfaces.Services;
using LoanService.Application.Mapping;
using LoanService.Domain.Enums;

namespace LoanService.Application.Commands.ApproveLoan;

public class ApproveLoanHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher)
    : IRequestHandler<ApproveLoanCommand, ApproveLoanResponse>
{
    public async Task<ApproveLoanResponse> Handle(
        ApproveLoanCommand request,
        CancellationToken cancellationToken)
    {
        var loan = await unitOfWork.Loans.GetByIdWithInstallmentsAsync(request.LoanId, cancellationToken)
            ?? throw new KeyNotFoundException("Loan not found.");

        if (loan.Status == ELoanStatus.Disbursed || loan.Status == ELoanStatus.Completed)
            throw new InvalidOperationException("Disbursed loan cannot be approved again.");

        if (loan.Status != ELoanStatus.Applied)
            throw new InvalidOperationException("Only applied loans can be approved.");

        await publisher.PublishAsync(
            new LoanDisbursementRequested(
                loan.Id,
                loan.AccountId,
                loan.Amount,
                loan.Currency,
                DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new ApproveLoanResponse
        {
            Message = "Loan disbursement requested.",
            Data = LoanMapper.ToDto(loan)
        };
    }
}
