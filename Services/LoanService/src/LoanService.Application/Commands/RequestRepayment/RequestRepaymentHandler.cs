using Banking.Contracts.Events;
using MediatR;
using LoanService.Application.Interfaces.Repositories;
using LoanService.Application.Interfaces.Services;
using LoanService.Application.Mapping;
using LoanService.Domain.Enums;

namespace LoanService.Application.Commands.RequestRepayment;

public class RequestRepaymentHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher)
    : IRequestHandler<RequestRepaymentCommand, RequestRepaymentResponse>
{
    public async Task<RequestRepaymentResponse> Handle(
        RequestRepaymentCommand request,
        CancellationToken cancellationToken)
    {
        var loan = await unitOfWork.Loans.GetByIdWithInstallmentsAsync(request.LoanId, cancellationToken)
            ?? throw new KeyNotFoundException("Loan not found.");

        if (loan.CustomerId != request.CustomerId)
            throw new InvalidOperationException("Loan does not belong to the caller.");

        if (loan.Status != ELoanStatus.Disbursed)
            throw new InvalidOperationException("Only disbursed loans can be repaid.");

        var installment = await unitOfWork.Installments.GetNextPendingAsync(loan.Id, cancellationToken)
            ?? throw new InvalidOperationException("No pending installment found.");

        await publisher.PublishAsync(
            new LoanRepaymentRequested(
                loan.Id,
                installment.Id,
                loan.AccountId,
                installment.Amount,
                loan.Currency,
                DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new RequestRepaymentResponse
        {
            Message = "Loan repayment requested.",
            Data = LoanMapper.ToDto(loan)
        };
    }
}
