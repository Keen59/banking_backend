using NotificationService.Application.Commands.RecordNotification;
using Xunit;

namespace NotificationService.UnitTests;

public class RecordNotificationHandlerTests
{
    [Fact]
    public async Task Handle_is_idempotent_on_type_and_source_id()
    {
        var store = new InMemoryNotificationUnitOfWork();
        var handler = new RecordNotificationHandler(store);
        var sourceId = Guid.NewGuid();
        var command = new RecordNotificationCommand
        {
            CustomerId = Guid.NewGuid(),
            Type = "kyc.approved",
            Title = "KYC onaylandı",
            Body = "Kimlik doğrulamanız onaylandı.",
            SourceId = sourceId,
            OccurredAt = DateTimeOffset.UtcNow
        };

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.Data!.Id, second.Data!.Id);
        Assert.Single(store.NotificationRows);
    }
}
