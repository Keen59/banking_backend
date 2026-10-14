using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.Helpers;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Interfaces.Services;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.CompleteIncomingEftPayment;

public class CompleteIncomingEftPaymentCommand : IRequest
{
    public required IncomingEftPaymentCompleted Event { get; init; }
}

public class CompleteIncomingEftPaymentHandler(IUnitOfWork unitOfWork, IIntegrationEventPublisher publisher)
    : IRequestHandler<CompleteIncomingEftPaymentCommand>
{
    public async Task Handle(CompleteIncomingEftPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.IncomingEftPayments.GetByIdAsync(request.Event.IncomingEftPaymentId);
        if (payment is null)
            return;

        if (payment.Status == ETransferStatus.Completed)
            return;

        payment.Status = ETransferStatus.Completed;
        payment.JournalEntryId = request.Event.JournalEntryId;
        payment.RejectReason = null;
        payment.UpdatedAt = request.Event.OccurredAt;
        unitOfWork.IncomingEftPayments.Update(payment);
        await publisher.PublishAsync(
            CustomerNotifications.IncomingEftCompleted(
                payment.AccountCustomerId,
                payment.Id,
                payment.Amount,
                request.Event.OccurredAt),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
