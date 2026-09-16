using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.RejectIncomingFastPayment;

public class RejectIncomingFastPaymentCommand : IRequest
{
    public required IncomingFastPaymentRejected Event { get; init; }
}

public class RejectIncomingFastPaymentHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RejectIncomingFastPaymentCommand>
{
    public async Task Handle(RejectIncomingFastPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.IncomingFastPayments.GetByIdAsync(request.Event.IncomingFastPaymentId);
        if (payment is null)
            return;

        if (payment.Status == ETransferStatus.Completed)
            return;

        if (payment.Status == ETransferStatus.Rejected)
            return;

        payment.Status = ETransferStatus.Rejected;
        payment.RejectReason = request.Event.Reason;
        payment.UpdatedAt = request.Event.OccurredAt;
        unitOfWork.IncomingFastPayments.Update(payment);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
