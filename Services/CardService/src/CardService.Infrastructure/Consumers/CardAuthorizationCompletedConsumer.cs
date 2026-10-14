using Banking.Contracts.Events;
using CardService.Application.Commands.CompleteAuthorization;
using MassTransit;
using MediatR;

namespace CardService.Infrastructure.Consumers;

public sealed class CardAuthorizationCompletedConsumer(IMediator mediator) : IConsumer<CardAuthorizationCompleted>
{
    public Task Consume(ConsumeContext<CardAuthorizationCompleted> context)
    {
        return mediator.Send(new CompleteAuthorizationCommand { Event = context.Message }, context.CancellationToken);
    }
}
