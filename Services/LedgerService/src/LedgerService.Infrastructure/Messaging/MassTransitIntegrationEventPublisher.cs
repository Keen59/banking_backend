using LedgerService.Application.Interfaces.Services;
using MassTransit;

namespace LedgerService.Infrastructure.Messaging;

public sealed class MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint)
    : IIntegrationEventPublisher
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        return publishEndpoint.Publish(message, cancellationToken);
    }
}
