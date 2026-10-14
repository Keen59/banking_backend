using NotificationService.Application.Commands.MarkNotificationRead;
using NotificationService.Domain.Entities;
using Xunit;

namespace NotificationService.UnitTests;

public class MarkNotificationReadHandlerTests
{
    [Fact]
    public async Task Handle_rejects_other_customer()
    {
        var ownerId = Guid.NewGuid();
        var notification = new CustomerNotification
        {
            Id = Guid.NewGuid(),
            CustomerId = ownerId,
            Type = "kyc.approved",
            Title = "KYC onaylandı",
            Body = "Kimlik doğrulamanız onaylandı.",
            SourceId = ownerId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var store = new InMemoryNotificationUnitOfWork();
        store.NotificationRows.Add(notification);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new MarkNotificationReadHandler(store).Handle(new MarkNotificationReadCommand
            {
                NotificationId = notification.Id,
                CustomerId = Guid.NewGuid()
            }, CancellationToken.None));

        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task Handle_throws_when_missing()
    {
        var store = new InMemoryNotificationUnitOfWork();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            new MarkNotificationReadHandler(store).Handle(new MarkNotificationReadCommand
            {
                NotificationId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid()
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_marks_owner_notification_read()
    {
        var ownerId = Guid.NewGuid();
        var notification = new CustomerNotification
        {
            Id = Guid.NewGuid(),
            CustomerId = ownerId,
            Type = "account.opened",
            Title = "Hesap açıldı",
            Body = "Hesabınız açıldı. IBAN: TR00.",
            SourceId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        var store = new InMemoryNotificationUnitOfWork();
        store.NotificationRows.Add(notification);

        var response = await new MarkNotificationReadHandler(store).Handle(new MarkNotificationReadCommand
        {
            NotificationId = notification.Id,
            CustomerId = ownerId
        }, CancellationToken.None);

        Assert.True(notification.IsRead);
        Assert.True(response.Data!.IsRead);
    }
}
