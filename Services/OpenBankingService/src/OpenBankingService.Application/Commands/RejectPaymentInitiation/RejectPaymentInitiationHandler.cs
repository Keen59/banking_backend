using MediatR;
using OpenBankingService.Application.Interfaces.Repositories;
using OpenBankingService.Domain.Enums;

namespace OpenBankingService.Application.Commands.RejectPaymentInitiation;

public class RejectPaymentInitiationCommand : IRequest
{
    public Guid InitiationId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }
}

public class RejectPaymentInitiationHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RejectPaymentInitiationCommand>
{
    public async Task Handle(RejectPaymentInitiationCommand request, CancellationToken cancellationToken)
    {
        var initiation = await unitOfWork.PaymentInitiations.GetByIdAsync(request.InitiationId);
        if (initiation is null)
            return;

        if (initiation.Status == EInitiationStatus.Completed)
            return;

        if (initiation.Status == EInitiationStatus.Rejected)
            return;

        initiation.Status = EInitiationStatus.Rejected;
        initiation.RejectReason = request.Reason;
        initiation.UpdatedAt = request.OccurredAt;
        unitOfWork.PaymentInitiations.Update(initiation);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
