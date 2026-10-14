using RiskService.Application.Commands.CloseAlert;
using RiskService.Application.Commands.GetOpenAlerts;
using RiskService.Domain.Entities;
using RiskService.Domain.Enums;
using Xunit;

namespace RiskService.UnitTests;

public class CloseAlertHandlerTests
{
    [Fact]
    public async Task Close_sets_closed_and_open_list_excludes_it()
    {
        var store = new InMemoryRiskUnitOfWork();
        var alert = OpenAlert();
        store.AlertRows.Add(alert);

        var closeHandler = new CloseAlertHandler(store);
        var response = await closeHandler.Handle(new CloseAlertCommand
        {
            AlertId = alert.Id,
            Reason = "Reviewed"
        }, CancellationToken.None);

        Assert.Equal(EAlertStatus.Closed, response.Data!.Status);
        Assert.Equal("Reviewed", response.Data.CloseReason);
        Assert.NotNull(response.Data.ClosedAt);

        var open = await new GetOpenAlertsHandler(store).Handle(
            new GetOpenAlertsCommand(),
            CancellationToken.None);

        Assert.Empty(open.Data);
    }

    [Fact]
    public async Task Closing_already_closed_throws()
    {
        var store = new InMemoryRiskUnitOfWork();
        var alert = OpenAlert();
        alert.Status = EAlertStatus.Closed;
        alert.CloseReason = "Done";
        alert.ClosedAt = DateTimeOffset.UtcNow;
        store.AlertRows.Add(alert);

        var handler = new CloseAlertHandler(store);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CloseAlertCommand
            {
                AlertId = alert.Id,
                Reason = "Again"
            }, CancellationToken.None));
    }

    private static RiskAlert OpenAlert()
        => new()
        {
            Id = Guid.NewGuid(),
            ScreeningId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Channel = ERiskChannel.Transfer,
            SourceId = Guid.NewGuid(),
            Rule = "LargeAmount",
            Amount = 25_000m,
            Status = EAlertStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
