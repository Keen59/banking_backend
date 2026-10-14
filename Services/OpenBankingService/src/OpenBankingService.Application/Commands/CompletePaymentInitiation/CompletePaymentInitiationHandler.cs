using Banking.Contracts.Events;
using MediatR;
using OpenBankingService.Application.Interfaces.Repositories;
using OpenBankingService.Domain.Enums;

namespace OpenBankingService.Application.Commands.CompletePaymentInitiation;

public class CompletePaymentInitiationCommand : IRequest
{
    public required TransferCompleted Event { get; init; }
}

public class CompletePaymentInitiationHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CompletePaymentInitiationCommand>
{
    public async Task Handle(CompletePaymentInitiationCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var initiation = await unitOfWork.PaymentInitiations.GetByIdAsync(evt.TransferId);
        if (initiation is null)
            return;

        if (initiation.Status == EInitiationStatus.Completed)
            return;

        initiation.Status = EInitiationStatus.Completed;
        initiation.JournalEntryId = evt.JournalEntryId;
        initiation.RejectReason = null;
        initiation.UpdatedAt = evt.OccurredAt;
        unitOfWork.PaymentInitiations.Update(initiation);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
