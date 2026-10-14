using Banking.Contracts.Events;
using MediatR;
using LoanService.Application.Helpers;
using LoanService.Application.Interfaces.Repositories;
using LoanService.Application.Interfaces.Services;
using LoanService.Domain.Entities;
using LoanService.Domain.Enums;

namespace LoanService.Application.Commands.CompleteDisbursement;

public class CompleteDisbursementCommand : IRequest
{
    public required LoanDisbursementPosted Event { get; init; }
}

public class CompleteDisbursementHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher)
    : IRequestHandler<CompleteDisbursementCommand>
{
    public async Task Handle(CompleteDisbursementCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var loan = await unitOfWork.Loans.GetByIdWithInstallmentsAsync(evt.LoanId, cancellationToken);
        if (loan is null)
            return;

        if (loan.Status is ELoanStatus.Disbursed or ELoanStatus.Completed)
            return;

        var now = evt.OccurredAt;
        loan.Status = ELoanStatus.Disbursed;
        loan.JournalEntryId = evt.JournalEntryId;
        loan.RemainingPrincipal = loan.Amount;
        loan.RejectReason = null;
        loan.UpdatedAt = now;
        unitOfWork.Loans.Update(loan);

        var today = DateTime.UtcNow.Date;
        for (var sequence = 1; sequence <= loan.TermMonths; sequence++)
        {
            var installment = new LoanInstallment
            {
                Id = Guid.NewGuid(),
                LoanId = loan.Id,
                Sequence = sequence,
                DueDate = new DateTimeOffset(today.AddMonths(sequence), TimeSpan.Zero),
                Amount = LoanInstallmentCalculator.AmountForSequence(loan.Amount, loan.TermMonths, sequence),
                Status = EInstallmentStatus.Pending,
                CreatedAt = now
            };
            loan.Installments.Add(installment);
            await unitOfWork.Installments.AddAsync(installment, cancellationToken);
        }

        await publisher.PublishAsync(
            CustomerNotifications.LoanDisbursed(loan.CustomerId, loan.Id, loan.Amount, now),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
