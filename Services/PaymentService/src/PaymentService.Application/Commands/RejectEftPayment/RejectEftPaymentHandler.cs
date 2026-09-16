using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.RejectEftPayment;

public class RejectEftPaymentCommand : IRequest
{
    public required EftPaymentRejected Event { get; init; }
}

public class RejectEftPaymentHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RejectEftPaymentCommand>
{
    public async Task Handle(RejectEftPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.EftPayments.GetByIdAsync(request.Event.EftPaymentId);
        if (payment is null)
            return;

        if (payment.Status == ETransferStatus.Completed)
            return;

        if (payment.Status == ETransferStatus.Rejected)
            return;

        payment.Status = ETransferStatus.Rejected;
        payment.RejectReason = request.Event.Reason;
        payment.UpdatedAt = request.Event.OccurredAt;
        unitOfWork.EftPayments.Update(payment);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
