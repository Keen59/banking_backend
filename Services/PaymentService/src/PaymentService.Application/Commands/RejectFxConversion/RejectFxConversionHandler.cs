using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.Helpers;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Interfaces.Services;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.RejectFxConversion;

public class RejectFxConversionCommand : IRequest
{
    public required FxConversionRejected Event { get; init; }
}

public class RejectFxConversionHandler(IUnitOfWork unitOfWork, IIntegrationEventPublisher publisher)
    : IRequestHandler<RejectFxConversionCommand>
{
    public async Task Handle(RejectFxConversionCommand request, CancellationToken cancellationToken)
    {
        var conversion = await unitOfWork.FxConversions.GetByIdAsync(request.Event.ConversionId);
        if (conversion is null)
            return;

        if (conversion.Status == ETransferStatus.Completed)
            return;

        if (conversion.Status == ETransferStatus.Rejected)
            return;

        conversion.Status = ETransferStatus.Rejected;
        conversion.RejectReason = request.Event.Reason;
        conversion.UpdatedAt = request.Event.OccurredAt;
        unitOfWork.FxConversions.Update(conversion);
        await publisher.PublishAsync(
            CustomerNotifications.FxRejected(
                conversion.CustomerId,
                conversion.Id,
                request.Event.Reason,
                request.Event.OccurredAt),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
