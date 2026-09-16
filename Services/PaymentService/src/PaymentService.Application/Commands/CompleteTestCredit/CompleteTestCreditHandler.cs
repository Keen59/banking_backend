using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.CompleteTestCredit;

public class CompleteTestCreditCommand : IRequest
{
    public required TestCreditPosted Event { get; init; }
}

public class CompleteTestCreditHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteTestCreditCommand>
{
    public async Task Handle(CompleteTestCreditCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var credit = await unitOfWork.TestCredits.GetByIdAsync(evt.CreditId);
        if (credit is null)
            return;

        if (credit.Status == ETransferStatus.Completed)
            return;

        credit.Status = ETransferStatus.Completed;
        credit.JournalEntryId = evt.JournalEntryId;
        credit.RejectReason = null;
        credit.UpdatedAt = evt.OccurredAt;
        unitOfWork.TestCredits.Update(credit);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
