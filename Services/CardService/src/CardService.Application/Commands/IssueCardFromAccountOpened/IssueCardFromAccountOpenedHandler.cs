using Banking.Contracts.Events;
using CardService.Application.Helpers;
using CardService.Application.Interfaces.Repositories;
using CardService.Application.Interfaces.Services;
using CardService.Domain.Entities;
using CardService.Domain.Enums;
using MediatR;

namespace CardService.Application.Commands.IssueCardFromAccountOpened;

public class IssueCardFromAccountOpenedCommand : IRequest
{
    public required AccountOpened Event { get; init; }
}

public class IssueCardFromAccountOpenedHandler(
    IUnitOfWork unitOfWork,
    IIntegrationEventPublisher publisher)
    : IRequestHandler<IssueCardFromAccountOpenedCommand>
{
    public async Task Handle(IssueCardFromAccountOpenedCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        if (await unitOfWork.Cards.ExistsByAccountIdAsync(evt.AccountId, cancellationToken))
            return;

        var now = evt.OccurredAt == default ? DateTimeOffset.UtcNow : evt.OccurredAt;
        var last4 = Random.Shared.Next(0, 10000).ToString("D4");
        var card = new Card
        {
            Id = Guid.NewGuid(),
            CustomerId = evt.CustomerId,
            AccountId = evt.AccountId,
            Token = Guid.NewGuid().ToString("N"),
            Last4 = last4,
            Brand = "Virtual",
            Status = ECardStatus.Active,
            CreatedAt = now
        };

        await unitOfWork.Cards.AddAsync(card, cancellationToken);
        await publisher.PublishAsync(
            CustomerNotifications.CardIssued(card.CustomerId, card.Id, card.Last4, now),
            cancellationToken);
        await unitOfWork.SaveAsync(cancellationToken);
    }
}
