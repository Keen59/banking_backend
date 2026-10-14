using Banking.Contracts.Events;
using MediatR;
using LoanService.Application.Helpers;
using LoanService.Application.Interfaces.Repositories;
using LoanService.Application.Interfaces.Services;
using LoanService.Domain.Enums;

namespace LoanService.Application.Commands.RejectDisbursement;

public class RejectDisbursementCommand : IRequest
{
    public required LoanDisbursementRejected Event { get; init; }
}

public class RejectDisbursementHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher)
    : IRequestHandler<RejectDisbursementCommand>
{
    public async Task Handle(RejectDisbursementCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var loan = await unitOfWork.Loans.GetByIdAsync(evt.LoanId);
        if (loan is null)
            return;

        if (loan.Status == ELoanStatus.Disbursed || loan.Status == ELoanStatus.Completed)
            return;

        if (loan.Status == ELoanStatus.Rejected)
            return;

        loan.Status = ELoanStatus.Rejected;
        loan.RejectReason = evt.Reason;
        loan.UpdatedAt = evt.OccurredAt;
        unitOfWork.Loans.Update(loan);

        await publisher.PublishAsync(
            CustomerNotifications.LoanRejected(loan.CustomerId, loan.Id, evt.Reason, evt.OccurredAt),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
