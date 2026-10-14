using Banking.Contracts.Events;
using CardService.Application.Commands.MarkAuthorizationHeld;
using MassTransit;
using MediatR;

namespace CardService.Infrastructure.Consumers;

public sealed class CardAuthorizationHeldConsumer(IMediator mediator) : IConsumer<CardAuthorizationHeld>
{
    public Task Consume(ConsumeContext<CardAuthorizationHeld> context)
    {
        return mediator.Send(new MarkAuthorizationHeldCommand { Event = context.Message }, context.CancellationToken);
    }
}
