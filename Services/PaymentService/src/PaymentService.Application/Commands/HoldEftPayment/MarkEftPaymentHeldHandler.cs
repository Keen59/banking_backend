using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.HoldEftPayment;

public class MarkEftPaymentHeldCommand : IRequest
{
    public required EftPaymentHeld Event { get; init; }
}

public class MarkEftPaymentHeldHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<MarkEftPaymentHeldCommand>
{
    public async Task Handle(MarkEftPaymentHeldCommand request, CancellationToken cancellationToken)
    {
        var payment = await unitOfWork.EftPayments.GetByIdAsync(request.Event.EftPaymentId);
        if (payment is null)
            return;

        if (payment.Status is ETransferStatus.Completed or ETransferStatus.Rejected or ETransferStatus.Held)
            return;

        payment.Status = ETransferStatus.Held;
        payment.UpdatedAt = request.Event.OccurredAt;
        unitOfWork.EftPayments.Update(payment);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
