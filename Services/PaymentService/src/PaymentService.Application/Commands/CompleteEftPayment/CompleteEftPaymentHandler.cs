using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.CompleteEftPayment;

public class CompleteEftPaymentCommand : IRequest
{
    public required EftPaymentCompleted Event { get; init; }
}

public class CompleteEftPaymentHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteEftPaymentCommand>
{
    public async Task Handle(CompleteEftPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.EftPayments.GetByIdAsync(request.Event.EftPaymentId);
        if (payment is null)
            return;

        if (payment.Status == ETransferStatus.Completed)
            return;

        payment.Status = ETransferStatus.Completed;
        payment.JournalEntryId = request.Event.JournalEntryId;
        payment.RejectReason = null;
        payment.UpdatedAt = request.Event.OccurredAt;
        unitOfWork.EftPayments.Update(payment);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
