using Banking.Contracts.Events;
using CardService.Application.Helpers;
using CardService.Application.Interfaces.Repositories;
using CardService.Application.Interfaces.Services;
using CardService.Domain.Enums;
using MediatR;

namespace CardService.Application.Commands.CompleteAuthorization;

public class CompleteAuthorizationCommand : IRequest
{
    public required CardAuthorizationCompleted Event { get; init; }
}

public class CompleteAuthorizationHandler(IUnitOfWork unitOfWork, IIntegrationEventPublisher publisher)
    : IRequestHandler<CompleteAuthorizationCommand>
{
    public async Task Handle(CompleteAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var authorization = await unitOfWork.CardAuthorizations.GetByIdAsync(request.Event.AuthorizationId);
        if (authorization is null)
            return;

        if (authorization.Status == EAuthorizationStatus.Completed)
            return;

        authorization.Status = EAuthorizationStatus.Completed;
        authorization.JournalEntryId = request.Event.JournalEntryId;
        authorization.RejectReason = null;
        authorization.UpdatedAt = request.Event.OccurredAt;
        unitOfWork.CardAuthorizations.Update(authorization);
        await publisher.PublishAsync(
            CustomerNotifications.CardCompleted(
                authorization.CustomerId,
                authorization.Id,
                authorization.Amount,
                request.Event.OccurredAt),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
