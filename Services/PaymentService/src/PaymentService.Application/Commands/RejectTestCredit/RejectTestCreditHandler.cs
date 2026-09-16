using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.RejectTestCredit;

public class RejectTestCreditCommand : IRequest
{
    public required TestCreditRejected Event { get; init; }
}

public class RejectTestCreditHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RejectTestCreditCommand>
{
    public async Task Handle(RejectTestCreditCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var credit = await unitOfWork.TestCredits.GetByIdAsync(evt.CreditId);
        if (credit is null)
            return;

        if (credit.Status == ETransferStatus.Completed)
            return;

        if (credit.Status == ETransferStatus.Rejected)
            return;

        credit.Status = ETransferStatus.Rejected;
        credit.RejectReason = evt.Reason;
        credit.UpdatedAt = evt.OccurredAt;
        unitOfWork.TestCredits.Update(credit);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
