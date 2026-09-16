using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.RejectFastPayment;

public class RejectFastPaymentCommand : IRequest
{
    public required FastPaymentRejected Event { get; init; }
}

public class RejectFastPaymentHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RejectFastPaymentCommand>
{
    public async Task Handle(RejectFastPaymentCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var payment = await unitOfWork.FastPayments.GetByIdAsync(evt.FastPaymentId);
        if (payment is null)
            return;

        if (payment.Status == ETransferStatus.Completed)
            return;

        if (payment.Status == ETransferStatus.Rejected)
            return;

        payment.Status = ETransferStatus.Rejected;
        payment.RejectReason = evt.Reason;
        payment.UpdatedAt = evt.OccurredAt;
        unitOfWork.FastPayments.Update(payment);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
