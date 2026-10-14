using Banking.Contracts.Events;
using MediatR;
using LoanService.Application.Helpers;
using LoanService.Application.Interfaces.Repositories;
using LoanService.Application.Interfaces.Services;
using LoanService.Domain.Enums;

namespace LoanService.Application.Commands.CompleteRepayment;

public class CompleteRepaymentCommand : IRequest
{
    public required LoanRepaymentPosted Event { get; init; }
}

public class CompleteRepaymentHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher)
    : IRequestHandler<CompleteRepaymentCommand>
{
    public async Task Handle(CompleteRepaymentCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var installment = await unitOfWork.Installments.GetByIdAsync(evt.InstallmentId);
        if (installment is null)
            return;

        if (installment.Status == EInstallmentStatus.Paid)
            return;

        var loan = await unitOfWork.Loans.GetByIdWithInstallmentsAsync(evt.LoanId, cancellationToken);
        if (loan is null)
            return;

        var now = evt.OccurredAt;
        installment.Status = EInstallmentStatus.Paid;
        installment.JournalEntryId = evt.JournalEntryId;
        installment.PaidAt = now;
        installment.UpdatedAt = now;
        unitOfWork.Installments.Update(installment);

        loan.RemainingPrincipal = Math.Max(0m, loan.RemainingPrincipal - installment.Amount);
        loan.UpdatedAt = now;

        var remainingPending = (await unitOfWork.Installments.GetByLoanIdAsync(loan.Id, cancellationToken))
            .Any(x => x.Status == EInstallmentStatus.Pending);
        if (!remainingPending)
            loan.Status = ELoanStatus.Completed;

        unitOfWork.Loans.Update(loan);

        await publisher.PublishAsync(
            CustomerNotifications.LoanRepaid(loan.CustomerId, loan.Id, installment.Amount, now),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
