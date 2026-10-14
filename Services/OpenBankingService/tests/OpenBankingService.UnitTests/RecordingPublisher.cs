using OpenBankingService.Application.Interfaces.Services;

namespace OpenBankingService.UnitTests;

internal sealed class RecordingPublisher : IIntegrationEventPublisher
{
    public List<object> Messages { get; } = [];

    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        Messages.Add(message);
        return Task.CompletedTask;
    }
}
