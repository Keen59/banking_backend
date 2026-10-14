using Banking.Contracts.Events;
using RiskService.Application.Commands.ProjectAccountOpened;
using Xunit;

namespace RiskService.UnitTests;

public class ProjectAccountOpenedHandlerTests
{
    [Fact]
    public async Task Handle_inserts_projection_once()
    {
        var accountId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var store = new InMemoryRiskUnitOfWork();
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
    }
}
