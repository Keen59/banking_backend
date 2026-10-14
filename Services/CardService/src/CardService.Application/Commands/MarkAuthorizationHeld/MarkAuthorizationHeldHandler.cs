using Banking.Contracts.Events;
using CardService.Application.Helpers;
using CardService.Application.Interfaces.Repositories;
using CardService.Application.Interfaces.Services;
using CardService.Domain.Enums;
using MediatR;

namespace CardService.Application.Commands.MarkAuthorizationHeld;

public class MarkAuthorizationHeldCommand : IRequest
{
    public required CardAuthorizationHeld Event { get; init; }
}

public class MarkAuthorizationHeldHandler(IUnitOfWork unitOfWork, IIntegrationEventPublisher publisher)
    : IRequestHandler<MarkAuthorizationHeldCommand>
{
    public async Task Handle(MarkAuthorizationHeldCommand request, CancellationToken cancellationToken)
    {
        var authorization = await unitOfWork.CardAuthorizations.GetByIdAsync(request.Event.AuthorizationId);
        if (authorization is null)
            return;

        if (authorization.Status is EAuthorizationStatus.Completed or EAuthorizationStatus.Rejected or EAuthorizationStatus.Held)
            return;

        authorization.Status = EAuthorizationStatus.Held;
        authorization.UpdatedAt = request.Event.OccurredAt;
        unitOfWork.CardAuthorizations.Update(authorization);
        await publisher.PublishAsync(
            CustomerNotifications.CardHeld(
                authorization.CustomerId,
                authorization.Id,
                authorization.Amount,
                request.Event.OccurredAt),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
