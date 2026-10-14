using Banking.Contracts.Events;
using CardService.Application.Interfaces.Repositories;
using CardService.Application.Interfaces.Services;
using CardService.Application.Mapping;
using CardService.Domain.Entities;
using CardService.Domain.Enums;
using MediatR;

namespace CardService.Application.Commands.AuthorizeCard;

public class AuthorizeCardHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher)
    : IRequestHandler<AuthorizeCardCommand, AuthorizeCardResponse>
{
    public const decimal MaxAmount = 50_000m;

    public async Task<AuthorizeCardResponse> Handle(
        AuthorizeCardCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.CardAuthorizations.GetByIdempotencyKeyAsync(
            request.CustomerId,
            request.IdempotencyKey,
            cancellationToken);

        if (existing is not null)
        {
            return new AuthorizeCardResponse
            {
                Message = "Existing card authorization returned for idempotency key.",
                Data = CardAuthorizationMapper.ToDto(existing)
            };
        }

        if (request.Amount > MaxAmount)
            throw new InvalidOperationException($"Amount exceeds per-authorization limit of {MaxAmount}.");

        var currency = request.Currency.ToUpperInvariant();
        if (!string.Equals(currency, "TRY", StringComparison.Ordinal))
            throw new InvalidOperationException("Currency must be TRY.");

        var card = await unitOfWork.Cards.GetByIdAsync(request.CardId)
            ?? throw new KeyNotFoundException("Card not found.");

        if (card.CustomerId != request.CustomerId)
            throw new InvalidOperationException("Card does not belong to the caller.");

        if (card.Status != ECardStatus.Active)
            throw new InvalidOperationException("Card is not active.");

        var now = DateTimeOffset.UtcNow;
        var authorization = new CardAuthorization
        {
            Id = Guid.NewGuid(),
            CardId = card.Id,
            CustomerId = card.CustomerId,
            AccountId = card.AccountId,
            Amount = request.Amount,
            Currency = currency,
            Merchant = request.Merchant,
            Description = request.Description,
            IdempotencyKey = request.IdempotencyKey,
            Status = EAuthorizationStatus.Initiated,
            CreatedAt = now
        };

        await unitOfWork.CardAuthorizations.AddAsync(authorization, cancellationToken);
        await publisher.PublishAsync(
            new CardAuthorizationRequested(
                authorization.Id,
                authorization.AccountId,
                authorization.Amount,
                authorization.Currency,
                authorization.Merchant,
                authorization.Description,
                now),
            cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        return new AuthorizeCardResponse
        {
            Message = "Card authorization initiated.",
            Data = CardAuthorizationMapper.ToDto(authorization)
        };
    }
}
