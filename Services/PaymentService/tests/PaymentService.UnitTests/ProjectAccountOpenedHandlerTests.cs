using Banking.Contracts.Events;
using PaymentService.Application.Commands.ProjectAccountOpened;
using PaymentService.Domain.Enums;
using Xunit;

namespace PaymentService.UnitTests;

public class ProjectAccountOpenedHandlerTests
{
    [Fact]
    public async Task Handle_inserts_active_projection_once()
    {
        var accountId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var store = new InMemoryPaymentUnitOfWork();
        var handler = new ProjectAccountOpenedHandler(store);
        var command = new ProjectAccountOpenedCommand
        {
            Event = new AccountOpened(
                accountId,
                customerId,
                "000000000001",
                "TR330010012345678901234567",
                "TRY",
                DateTimeOffset.UtcNow)
        };

        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        var projection = Assert.Single(store.Accounts);
        Assert.Equal(accountId, projection.Id);
        Assert.Equal(customerId, projection.CustomerId);
        Assert.Equal("TRY", projection.Currency);
        Assert.Equal(EAccountProjectionStatus.Active, projection.Status);
    }
}
