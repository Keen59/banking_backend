using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.CompleteFastPayment;

public class CompleteFastPaymentCommand : IRequest
{
    public required FastPaymentCompleted Event { get; init; }
}

public class CompleteFastPaymentHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteFastPaymentCommand>
{
    public async Task Handle(CompleteFastPaymentCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var payment = await unitOfWork.FastPayments.GetByIdAsync(evt.FastPaymentId);
        if (payment is null)
            return;

        if (payment.Status == ETransferStatus.Completed)
            return;

        payment.Status = ETransferStatus.Completed;
        payment.JournalEntryId = evt.JournalEntryId;
        payment.RejectReason = null;
        payment.UpdatedAt = evt.OccurredAt;
        unitOfWork.FastPayments.Update(payment);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
