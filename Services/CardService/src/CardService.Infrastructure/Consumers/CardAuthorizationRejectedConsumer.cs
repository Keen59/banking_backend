using Banking.Contracts.Events;
using CardService.Application.Commands.RejectAuthorization;
using MassTransit;
using MediatR;

namespace CardService.Infrastructure.Consumers;

public sealed class CardAuthorizationRejectedConsumer(IMediator mediator) : IConsumer<CardAuthorizationRejected>
{
    public Task Consume(ConsumeContext<CardAuthorizationRejected> context)
    {
        return mediator.Send(new RejectAuthorizationCommand { Event = context.Message }, context.CancellationToken);
    }
}
