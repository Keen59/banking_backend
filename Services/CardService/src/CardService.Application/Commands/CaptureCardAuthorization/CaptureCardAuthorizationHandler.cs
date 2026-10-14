using Banking.Contracts.Events;
using CardService.Application.DTOs;
using CardService.Application.DTOs.CardAuthorizations;
using CardService.Application.Interfaces.Repositories;
using CardService.Application.Interfaces.Services;
using CardService.Application.Mapping;
using CardService.Domain.Enums;
using MediatR;

namespace CardService.Application.Commands.CaptureCardAuthorization;

public class CaptureCardAuthorizationCommand : IRequest<CaptureCardAuthorizationResponse>
{
    public Guid CustomerId { get; set; }

    public Guid CardId { get; set; }

    public Guid AuthorizationId { get; set; }
}

public class CaptureCardAuthorizationResponse : Response
{
    public CardAuthorizationDto? Data { get; set; }
}

public class CaptureCardAuthorizationHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher)
    : IRequestHandler<CaptureCardAuthorizationCommand, CaptureCardAuthorizationResponse>
{
    public async Task<CaptureCardAuthorizationResponse> Handle(
        CaptureCardAuthorizationCommand request,
        CancellationToken cancellationToken)
    {
        var authorization = await GetOwnedAuthorizationAsync(unitOfWork, request.CardId, request.AuthorizationId, request.CustomerId);

        if (authorization.Status == EAuthorizationStatus.Completed)
        {
            return new CaptureCardAuthorizationResponse
            {
                Message = "Card authorization already captured.",
                Data = CardAuthorizationMapper.ToDto(authorization)
            };
        }

        if (authorization.Status == EAuthorizationStatus.Rejected)
            throw new InvalidOperationException("Rejected card authorization cannot be captured.");

        if (authorization.Status != EAuthorizationStatus.Held)
            throw new InvalidOperationException("Card authorization hold is not in place yet.");

        await publisher.PublishAsync(
            new CardCaptureRequested(authorization.Id, authorization.Description, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new CaptureCardAuthorizationResponse
        {
            Message = "Card capture requested.",
            Data = CardAuthorizationMapper.ToDto(authorization)
        };
    }

    internal static async Task<Domain.Entities.CardAuthorization> GetOwnedAuthorizationAsync(
        IUnitOfWork unitOfWork,
        Guid cardId,
        Guid authorizationId,
        Guid customerId)
    {
        var card = await unitOfWork.Cards.GetByIdAsync(cardId)
            ?? throw new KeyNotFoundException("Card not found.");

        if (card.CustomerId != customerId)
            throw new InvalidOperationException("Card does not belong to the caller.");

        var authorization = await unitOfWork.CardAuthorizations.GetByIdAsync(authorizationId)
            ?? throw new KeyNotFoundException("Card authorization not found.");

        if (authorization.CardId != card.Id)
            throw new KeyNotFoundException("Card authorization not found.");

        return authorization;
    }
}
