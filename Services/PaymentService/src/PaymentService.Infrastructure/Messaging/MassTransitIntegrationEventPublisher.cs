using PaymentService.Application.Interfaces.Services;
using MassTransit;

namespace PaymentService.Infrastructure.Messaging;

public sealed class MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint)
    : IIntegrationEventPublisher
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        return publishEndpoint.Publish(message, cancellationToken);
    }
}
