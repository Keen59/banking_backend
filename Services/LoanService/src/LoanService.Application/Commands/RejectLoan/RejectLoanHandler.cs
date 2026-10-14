using MediatR;
using LoanService.Application.Helpers;
using LoanService.Application.Interfaces.Repositories;
using LoanService.Application.Interfaces.Services;
using LoanService.Application.Mapping;
using LoanService.Domain.Enums;

namespace LoanService.Application.Commands.RejectLoan;

public class RejectLoanHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher)
    : IRequestHandler<RejectLoanCommand, RejectLoanResponse>
{
    public async Task<RejectLoanResponse> Handle(
        RejectLoanCommand request,
        CancellationToken cancellationToken)
    {
        var loan = await unitOfWork.Loans.GetByIdWithInstallmentsAsync(request.LoanId, cancellationToken)
            ?? throw new KeyNotFoundException("Loan not found.");

        if (loan.Status == ELoanStatus.Rejected)
        {
            return new RejectLoanResponse
            {
                Message = "Loan already rejected.",
                Data = LoanMapper.ToDto(loan)
            };
        }

        if (loan.Status == ELoanStatus.Disbursed || loan.Status == ELoanStatus.Completed)
            throw new InvalidOperationException("Disbursed loan cannot be rejected.");

        if (loan.Status != ELoanStatus.Applied)
            throw new InvalidOperationException("Only applied loans can be rejected.");

        var now = DateTimeOffset.UtcNow;
        loan.Status = ELoanStatus.Rejected;
        loan.RejectReason = request.Reason;
        loan.UpdatedAt = now;
        unitOfWork.Loans.Update(loan);

        await publisher.PublishAsync(
            CustomerNotifications.LoanRejected(loan.CustomerId, loan.Id, request.Reason, now),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new RejectLoanResponse
        {
            Message = "Loan rejected.",
            Data = LoanMapper.ToDto(loan)
        };
    }
}
