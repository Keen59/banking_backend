using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.Helpers;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Interfaces.Services;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.RejectIncomingEftPayment;

public class RejectIncomingEftPaymentCommand : IRequest
{
    public required IncomingEftPaymentRejected Event { get; init; }
}

public class RejectIncomingEftPaymentHandler(IUnitOfWork unitOfWork, IIntegrationEventPublisher publisher)
    : IRequestHandler<RejectIncomingEftPaymentCommand>
{
    public async Task Handle(RejectIncomingEftPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.IncomingEftPayments.GetByIdAsync(request.Event.IncomingEftPaymentId);
        if (payment is null)
            return;

        if (payment.Status == ETransferStatus.Completed)
            return;

        if (payment.Status == ETransferStatus.Rejected)
            return;

        payment.Status = ETransferStatus.Rejected;
        payment.RejectReason = request.Event.Reason;
        payment.UpdatedAt = request.Event.OccurredAt;
        unitOfWork.IncomingEftPayments.Update(payment);
        await publisher.PublishAsync(
            CustomerNotifications.IncomingEftRejected(
                payment.AccountCustomerId,
                payment.Id,
                request.Event.Reason,
                request.Event.OccurredAt),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
