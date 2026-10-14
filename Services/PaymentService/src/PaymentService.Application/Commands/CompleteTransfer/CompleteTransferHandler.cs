using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.Helpers;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Interfaces.Services;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.CompleteTransfer;

public class CompleteTransferCommand : IRequest
{
    public required TransferCompleted Event { get; init; }
}

public class CompleteTransferHandler(IUnitOfWork unitOfWork, IIntegrationEventPublisher publisher)
    : IRequestHandler<CompleteTransferCommand>
{
    public async Task Handle(CompleteTransferCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var transfer = await unitOfWork.Transfers.GetByIdAsync(evt.TransferId);
        if (transfer is null)
            return;

        if (transfer.Status == ETransferStatus.Completed)
            return;

        transfer.Status = ETransferStatus.Completed;
        transfer.JournalEntryId = evt.JournalEntryId;
        transfer.RejectReason = null;
        transfer.UpdatedAt = evt.OccurredAt;
        unitOfWork.Transfers.Update(transfer);
        await publisher.PublishAsync(
            CustomerNotifications.TransferCompleted(transfer.CustomerId, transfer.Id, transfer.Amount, evt.OccurredAt),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
