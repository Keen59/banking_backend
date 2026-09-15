using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.RejectTransfer;

public class RejectTransferCommand : IRequest
{
    public required TransferRejected Event { get; init; }
}

public class RejectTransferHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RejectTransferCommand>
{
    public async Task Handle(RejectTransferCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var transfer = await unitOfWork.Transfers.GetByIdAsync(evt.TransferId);
        if (transfer is null)
            return;

        if (transfer.Status == ETransferStatus.Completed)
            return;

        if (transfer.Status == ETransferStatus.Rejected)
            return;

        transfer.Status = ETransferStatus.Rejected;
        transfer.RejectReason = evt.Reason;
        transfer.UpdatedAt = evt.OccurredAt;
        unitOfWork.Transfers.Update(transfer);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
