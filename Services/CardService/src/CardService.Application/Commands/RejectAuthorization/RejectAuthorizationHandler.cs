using Banking.Contracts.Events;
using CardService.Application.Helpers;
using CardService.Application.Interfaces.Repositories;
using CardService.Application.Interfaces.Services;
using CardService.Domain.Enums;
using MediatR;

namespace CardService.Application.Commands.RejectAuthorization;

public class RejectAuthorizationCommand : IRequest
{
    public required CardAuthorizationRejected Event { get; init; }
}

public class RejectAuthorizationHandler(IUnitOfWork unitOfWork, IIntegrationEventPublisher publisher)
    : IRequestHandler<RejectAuthorizationCommand>
{
    public async Task Handle(RejectAuthorizationCommand request, CancellationToken cancellationToken)
    {
        var authorization = await unitOfWork.CardAuthorizations.GetByIdAsync(request.Event.AuthorizationId);
        if (authorization is null)
            return;

        if (authorization.Status == EAuthorizationStatus.Completed)
            return;

        if (authorization.Status == EAuthorizationStatus.Rejected)
            return;

        authorization.Status = EAuthorizationStatus.Rejected;
        authorization.RejectReason = request.Event.Reason;
        authorization.UpdatedAt = request.Event.OccurredAt;
        unitOfWork.CardAuthorizations.Update(authorization);
        await publisher.PublishAsync(
            CustomerNotifications.CardRejected(
                authorization.CustomerId,
                authorization.Id,
                authorization.Amount,
                request.Event.OccurredAt),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
