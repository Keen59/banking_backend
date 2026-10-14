using NotificationService.Application.Commands.GetMyNotifications;
using NotificationService.Domain.Entities;
using Xunit;

namespace NotificationService.UnitTests;

public class GetMyNotificationsHandlerTests
{
    [Fact]
    public async Task Handle_returns_only_that_customer()
    {
        var customerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var store = new InMemoryNotificationUnitOfWork();
        store.NotificationRows.Add(new CustomerNotification
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Type = "kyc.approved",
            Title = "KYC onaylandı",
            Body = "Kimlik doğrulamanız onaylandı.",
            SourceId = customerId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        store.NotificationRows.Add(new CustomerNotification
        {
            Id = Guid.NewGuid(),
            CustomerId = otherCustomerId,
            Type = "account.opened",
            Title = "Hesap açıldı",
            Body = "Hesabınız açıldı. IBAN: TR00.",
            SourceId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        });

        var response = await new GetMyNotificationsHandler(store).Handle(
            new GetMyNotificationsCommand { CustomerId = customerId },
            CancellationToken.None);

        var item = Assert.Single(response.Data);
        Assert.Equal(customerId, item.CustomerId);
        Assert.Equal("kyc.approved", item.Type);
    }
}
