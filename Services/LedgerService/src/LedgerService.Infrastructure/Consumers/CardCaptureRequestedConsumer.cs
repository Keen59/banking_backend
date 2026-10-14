using Banking.Contracts.Events;
using LedgerService.Application.Commands.CaptureCardAuthorization;
using MassTransit;
using MediatR;

namespace LedgerService.Infrastructure.Consumers;

public sealed class CardCaptureRequestedConsumer(IMediator mediator) : IConsumer<CardCaptureRequested>
{
    public Task Consume(ConsumeContext<CardCaptureRequested> context)
    {
        return mediator.Send(new CaptureCardAuthorizationCommand
        {
            AuthorizationId = context.Message.AuthorizationId,
            Description = context.Message.Description
        }, context.CancellationToken);
    }
}
