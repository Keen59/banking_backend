using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.CompleteIncomingFastPayment;

public class CompleteIncomingFastPaymentCommand : IRequest
{
    public required IncomingFastPaymentCompleted Event { get; init; }
}

public class CompleteIncomingFastPaymentHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteIncomingFastPaymentCommand>
{
    public async Task Handle(CompleteIncomingFastPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.IncomingFastPayments.GetByIdAsync(request.Event.IncomingFastPaymentId);
        if (payment is null)
            return;

        if (payment.Status == ETransferStatus.Completed)
            return;

        payment.Status = ETransferStatus.Completed;
        payment.JournalEntryId = request.Event.JournalEntryId;
        payment.RejectReason = null;
        payment.UpdatedAt = request.Event.OccurredAt;
        unitOfWork.IncomingFastPayments.Update(payment);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
