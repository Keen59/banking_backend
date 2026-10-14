using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using NotificationService.Application.Commands.RecordNotification;

namespace NotificationService.Infrastructure.Consumers;

public sealed class KycApprovedConsumer(IMediator mediator) : IConsumer<KycApproved>
{
    public Task Consume(ConsumeContext<KycApproved> context)
    {
        var message = context.Message;
        return mediator.Send(new RecordNotificationCommand
        {
            CustomerId = message.CustomerId,
            Type = "kyc.approved",
            Title = "KYC onaylandı",
            Body = "Kimlik doğrulamanız onaylandı.",
            SourceId = message.CustomerId,
            OccurredAt = message.OccurredAt
        }, context.CancellationToken);
    }
}
