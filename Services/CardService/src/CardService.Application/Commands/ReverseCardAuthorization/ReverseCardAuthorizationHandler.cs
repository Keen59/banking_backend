using Banking.Contracts.Events;
using CardService.Application.Commands.CaptureCardAuthorization;
using CardService.Application.DTOs;
using CardService.Application.DTOs.CardAuthorizations;
using CardService.Application.Interfaces.Repositories;
using CardService.Application.Interfaces.Services;
using CardService.Application.Mapping;
using CardService.Domain.Enums;
using MediatR;

namespace CardService.Application.Commands.ReverseCardAuthorization;

public class ReverseCardAuthorizationCommand : IRequest<ReverseCardAuthorizationResponse>
{
    public Guid CustomerId { get; set; }

    public Guid CardId { get; set; }

    public Guid AuthorizationId { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public class ReverseCardAuthorizationResponse : Response
{
    public CardAuthorizationDto? Data { get; set; }
}

public class ReverseCardAuthorizationHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher)
    : IRequestHandler<ReverseCardAuthorizationCommand, ReverseCardAuthorizationResponse>
{
    public async Task<ReverseCardAuthorizationResponse> Handle(
        ReverseCardAuthorizationCommand request,
        CancellationToken cancellationToken)
    {
        var authorization = await CaptureCardAuthorizationHandler.GetOwnedAuthorizationAsync(
            unitOfWork,
            request.CardId,
            request.AuthorizationId,
            request.CustomerId);

        if (authorization.Status == EAuthorizationStatus.Rejected)
        {
            return new ReverseCardAuthorizationResponse
            {
                Message = "Card authorization already reversed.",
                Data = CardAuthorizationMapper.ToDto(authorization)
            };
        }

        if (authorization.Status == EAuthorizationStatus.Completed)
            throw new InvalidOperationException("Captured card authorization cannot be reversed.");

        if (authorization.Status != EAuthorizationStatus.Held)
            throw new InvalidOperationException("Card authorization hold is not in place yet.");

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "Kart yetkisi iptal." : request.Reason.Trim();
        await publisher.PublishAsync(
            new CardAuthorizationReverseRequested(authorization.Id, reason, DateTimeOffset.UtcNow),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);

        return new ReverseCardAuthorizationResponse
        {
            Message = "Card authorization reverse requested.",
            Data = CardAuthorizationMapper.ToDto(authorization)
        };
    }
}
