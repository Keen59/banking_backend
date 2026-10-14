using Banking.Contracts.Events;
using LedgerService.Application.Commands.HoldCardAuthorization;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Application.Interfaces.Services;
using MassTransit;
using MediatR;

namespace LedgerService.Infrastructure.Consumers;

public sealed class CardAuthorizationRequestedConsumer(
    IMediator mediator,
    IIntegrationEventPublisher eventPublisher,
    IUnitOfWork unitOfWork) : IConsumer<CardAuthorizationRequested>
{
    public async Task Consume(ConsumeContext<CardAuthorizationRequested> context)
    {
        var message = context.Message;
        try
        {
            await mediator.Send(new HoldCardAuthorizationCommand
            {
                AuthorizationId = message.AuthorizationId,
                AccountId = message.AccountId,
                Amount = message.Amount,
                Currency = message.Currency,
                Merchant = message.Merchant
            }, context.CancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            await eventPublisher.PublishAsync(
                new CardAuthorizationRejected(message.AuthorizationId, exception.Message, DateTimeOffset.UtcNow),
                context.CancellationToken);
            await unitOfWork.SaveAsync(context.CancellationToken);
        }
    }
}
