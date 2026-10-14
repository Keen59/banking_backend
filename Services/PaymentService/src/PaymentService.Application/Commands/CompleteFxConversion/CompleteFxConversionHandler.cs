using Banking.Contracts.Events;
using MediatR;
using PaymentService.Application.Helpers;
using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Application.Interfaces.Services;
using PaymentService.Domain.Enums;

namespace PaymentService.Application.Commands.CompleteFxConversion;

public class CompleteFxConversionCommand : IRequest
{
    public required FxConversionCompleted Event { get; init; }
}

public class CompleteFxConversionHandler(IUnitOfWork unitOfWork, IIntegrationEventPublisher publisher)
    : IRequestHandler<CompleteFxConversionCommand>
{
    public async Task Handle(CompleteFxConversionCommand request, CancellationToken cancellationToken)
    {
        var conversion = await unitOfWork.FxConversions.GetByIdAsync(request.Event.ConversionId);
        if (conversion is null)
            return;

        if (conversion.Status == ETransferStatus.Completed)
            return;

        conversion.Status = ETransferStatus.Completed;
        conversion.SellJournalEntryId = request.Event.SellJournalEntryId;
        conversion.BuyJournalEntryId = request.Event.BuyJournalEntryId;
        conversion.RejectReason = null;
        conversion.UpdatedAt = request.Event.OccurredAt;
        unitOfWork.FxConversions.Update(conversion);
        await publisher.PublishAsync(
            CustomerNotifications.FxCompleted(
                conversion.CustomerId,
                conversion.Id,
                conversion.SourceAmount,
                conversion.SourceCurrency,
                conversion.DestinationAmount,
                conversion.DestinationCurrency,
                request.Event.OccurredAt),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
