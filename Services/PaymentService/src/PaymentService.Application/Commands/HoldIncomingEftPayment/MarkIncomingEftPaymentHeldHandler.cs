using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.Helpers;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Interfaces.Services;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.HoldIncomingEftPayment;

public class MarkIncomingEftPaymentHeldCommand : IRequest
{
    public required IncomingEftPaymentHeld Event { get; init; }
}

public class MarkIncomingEftPaymentHeldHandler(IUnitOfWork unitOfWork, IIntegrationEventPublisher publisher)
    : IRequestHandler<MarkIncomingEftPaymentHeldCommand>
{
    public async Task Handle(MarkIncomingEftPaymentHeldCommand request, CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.IncomingEftPayments.GetByIdAsync(request.Event.IncomingEftPaymentId);
        if (payment is null)
            return;

        if (payment.Status is ETransferStatus.Completed or ETransferStatus.Rejected or ETransferStatus.Held)
            return;

        payment.Status = ETransferStatus.Held;
        payment.UpdatedAt = request.Event.OccurredAt;
        unitOfWork.IncomingEftPayments.Update(payment);
        await publisher.PublishAsync(
            CustomerNotifications.IncomingEftHeld(
                payment.AccountCustomerId,
                payment.Id,
                payment.Amount,
                request.Event.OccurredAt),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
