using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using NotificationService.Application.Commands.RecordNotification;

namespace NotificationService.Infrastructure.Consumers;

public sealed class NotificationRequestedConsumer(IMediator mediator) : IConsumer<NotificationRequested>
{
    public Task Consume(ConsumeContext<NotificationRequested> context)
    {
        var message = context.Message;
        return mediator.Send(new RecordNotificationCommand
        {
            CustomerId = message.CustomerId,
            Type = message.Type,
            Title = message.Title,
            Body = message.Body,
            SourceId = message.SourceId,
            OccurredAt = message.OccurredAt
        }, context.CancellationToken);
    }
}
