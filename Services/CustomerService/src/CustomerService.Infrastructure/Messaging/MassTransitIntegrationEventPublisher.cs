using CustomerService.Application.Interfaces.Services;
using MassTransit;

namespace CustomerService.Infrastructure.Messaging;

public sealed class MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint)
    : IIntegrationEventPublisher
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        return publishEndpoint.Publish(message, cancellationToken);
    }
}
