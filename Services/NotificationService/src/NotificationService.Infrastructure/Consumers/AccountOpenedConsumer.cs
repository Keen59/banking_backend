using Banking.Contracts.Events;
using MassTransit;
using MediatR;
using NotificationService.Application.Commands.RecordNotification;

namespace NotificationService.Infrastructure.Consumers;

public sealed class AccountOpenedConsumer(IMediator mediator) : IConsumer<AccountOpened>
{
    public Task Consume(ConsumeContext<AccountOpened> context)
    {
        var message = context.Message;
        return mediator.Send(new RecordNotificationCommand
        {
            CustomerId = message.CustomerId,
            Type = "account.opened",
            Title = "Hesap açıldı",
            Body = $"Hesabınız açıldı. IBAN: {message.Iban}.",
            SourceId = message.AccountId,
            OccurredAt = message.OccurredAt
        }, context.CancellationToken);
    }
}
