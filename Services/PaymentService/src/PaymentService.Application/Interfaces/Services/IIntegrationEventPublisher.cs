namespace PaymentService.Application.Interfaces.Services;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class;
}
