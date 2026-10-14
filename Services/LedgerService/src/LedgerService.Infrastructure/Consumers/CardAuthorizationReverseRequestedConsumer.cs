using Banking.Contracts.Events;
using LedgerService.Application.Commands.ReverseCardAuthorization;
using MassTransit;
using MediatR;

namespace LedgerService.Infrastructure.Consumers;

public sealed class CardAuthorizationReverseRequestedConsumer(IMediator mediator)
    : IConsumer<CardAuthorizationReverseRequested>
{
    public Task Consume(ConsumeContext<CardAuthorizationReverseRequested> context)
    {
        return mediator.Send(new ReverseCardAuthorizationCommand
        {
            AuthorizationId = context.Message.AuthorizationId,
            Reason = context.Message.Reason
        }, context.CancellationToken);
    }
}
