using Microsoft.Extensions.Options;
using RiskService.Application.Commands.EvaluatePayment;
using RiskService.Application.Options;
using RiskService.Domain;
using RiskService.Domain.Entities;
using RiskService.Domain.Enums;
using Xunit;

namespace RiskService.UnitTests;

public class EvaluatePaymentHandlerTests
{
    [Fact]
    public async Task Transfer_at_flag_amount_opens_large_amount_alert()
    {
        var store = SeededStore(out var accountId);
        var handler = Handler(store);

        await handler.Handle(Transfer(accountId, 25_000m), CancellationToken.None);

        Assert.Single(store.ScreeningRows);
        var alert = Assert.Single(store.AlertRows);
        Assert.Equal(RiskRules.LargeAmount, alert.Rule);
        Assert.Equal(EAlertStatus.Open, alert.Status);
    }

    [Fact]
    public async Task Transfer_below_flag_amount_creates_screening_without_alert()
    {
        var store = SeededStore(out var accountId);
        var handler = Handler(store);

        await handler.Handle(Transfer(accountId, 24_999m), CancellationToken.None);

        Assert.Single(store.ScreeningRows);
        Assert.Empty(store.AlertRows);
    }

    [Fact]
    public async Task Same_transfer_id_second_evaluate_is_noop()
    {
        var store = SeededStore(out var accountId);
        var handler = Handler(store);
        var transferId = Guid.NewGuid();
        var command = Transfer(accountId, 25_000m, transferId);

        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        Assert.Single(store.ScreeningRows);
        Assert.Single(store.AlertRows);
    }

    [Fact]
    public async Task Missing_account_projection_skips_screening()
    {
        var store = new InMemoryRiskUnitOfWork();
        var handler = Handler(store);

        await handler.Handle(Transfer(Guid.NewGuid(), 25_000m), CancellationToken.None);

        Assert.Empty(store.ScreeningRows);
        Assert.Empty(store.AlertRows);
    }

    [Fact]
    public async Task Third_outbound_30000_same_day_opens_daily_velocity()
    {
        var store = SeededStore(out var accountId);
        var handler = Handler(store);

        await handler.Handle(Transfer(accountId, 30_000m), CancellationToken.None);
        await handler.Handle(Transfer(accountId, 30_000m), CancellationToken.None);
        await handler.Handle(Transfer(accountId, 30_000m), CancellationToken.None);

        Assert.Equal(3, store.ScreeningRows.Count);
        Assert.Equal(3, store.AlertRows.Count(x => x.Rule == RiskRules.LargeAmount));
        Assert.Single(store.AlertRows, x => x.Rule == RiskRules.DailyVelocity);
        Assert.Equal(4, store.AlertRows.Count);
    }

    [Fact]
    public async Task Incoming_fast_30000_opens_large_amount_but_not_daily_velocity()
    {
        var store = SeededStore(out var accountId);
        var handler = Handler(store);

        await handler.Handle(new EvaluatePaymentCommand
        {
            Channel = ERiskChannel.IncomingFast,
            SourceId = Guid.NewGuid(),
            AccountId = accountId,
            Amount = 30_000m,
            Currency = "TRY",
            Direction = ERiskDirection.Inbound
        }, CancellationToken.None);

        Assert.Single(store.ScreeningRows);
        var alert = Assert.Single(store.AlertRows);
        Assert.Equal(RiskRules.LargeAmount, alert.Rule);
        Assert.DoesNotContain(store.AlertRows, x => x.Rule == RiskRules.DailyVelocity);
    }

    [Fact]
    public async Task Fx_try_equivalent_at_flag_amount_opens_large_amount()
    {
        var store = SeededStore(out var accountId);
        var handler = Handler(store);

        await handler.Handle(new EvaluatePaymentCommand
        {
            Channel = ERiskChannel.Fx,
            SourceId = Guid.NewGuid(),
            AccountId = accountId,
            Amount = 25_000m,
            Currency = "TRY",
            Direction = ERiskDirection.Outbound
        }, CancellationToken.None);

        var alert = Assert.Single(store.AlertRows);
        Assert.Equal(RiskRules.LargeAmount, alert.Rule);
        Assert.Equal(ERiskChannel.Fx, alert.Channel);
    }

    [Fact]
    public async Task Third_outbound_just_below_flag_opens_structuring()
    {
        var store = SeededStore(out var accountId);
        var handler = Handler(store);

        await handler.Handle(Transfer(accountId, 24_000m), CancellationToken.None);
        await handler.Handle(Transfer(accountId, 24_000m), CancellationToken.None);
        await handler.Handle(Transfer(accountId, 24_000m), CancellationToken.None);

        Assert.Equal(3, store.ScreeningRows.Count);
        Assert.DoesNotContain(store.AlertRows, x => x.Rule == RiskRules.LargeAmount);
        Assert.DoesNotContain(store.AlertRows, x => x.Rule == RiskRules.DailyVelocity);
        Assert.Single(store.AlertRows, x => x.Rule == RiskRules.Structuring);
    }

    [Fact]
    public async Task Watchlist_national_id_opens_watchlist_hit()
    {
        var store = SeededStore(out var accountId);
        store.ProfileRows.Add(new CustomerProfile
        {
            Id = store.Accounts[0].CustomerId,
            CifNumber = "000000000001",
            FirstName = "Ali",
            LastName = "Yilmaz",
            NationalId = "99999999990"
        });
        var handler = Handler(store);

        await handler.Handle(Transfer(accountId, 100m), CancellationToken.None);

        var alert = Assert.Single(store.AlertRows);
        Assert.Equal(RiskRules.WatchlistHit, alert.Rule);
    }

    [Fact]
    public async Task Loan_at_flag_amount_opens_large_amount()
    {
        var store = SeededStore(out var accountId);
        var handler = Handler(store);

        await handler.Handle(new EvaluatePaymentCommand
        {
            Channel = ERiskChannel.Loan,
            SourceId = Guid.NewGuid(),
            AccountId = accountId,
            Amount = 25_000m,
            Currency = "TRY",
            Direction = ERiskDirection.Outbound
        }, CancellationToken.None);

        var alert = Assert.Single(store.AlertRows);
        Assert.Equal(RiskRules.LargeAmount, alert.Rule);
        Assert.Equal(ERiskChannel.Loan, alert.Channel);
    }

    private static InMemoryRiskUnitOfWork SeededStore(out Guid accountId)
    {
        accountId = Guid.NewGuid();
        var store = new InMemoryRiskUnitOfWork();
        store.Accounts.Add(new AccountProjection
        {
            Id = accountId,
            CustomerId = Guid.NewGuid(),
            Currency = "TRY",
            CreatedAt = DateTimeOffset.UtcNow
        });
        return store;
    }

    private static EvaluatePaymentHandler Handler(InMemoryRiskUnitOfWork store)
        => new(store, Limits(), Watchlist());

    private static EvaluatePaymentCommand Transfer(Guid accountId, decimal amount, Guid? sourceId = null)
        => new()
        {
            Channel = ERiskChannel.Transfer,
            SourceId = sourceId ?? Guid.NewGuid(),
            AccountId = accountId,
            Amount = amount,
            Currency = "TRY",
            Direction = ERiskDirection.Outbound
        };

    private static IOptions<RiskLimitOptions> Limits()
        => Options.Create(new RiskLimitOptions
        {
            FlagAmount = 25_000m,
            DailyAmount = 80_000m,
            StructuringFloor = 20_000m,
            StructuringCount = 3
        });

    private static IOptions<WatchlistOptions> Watchlist()
        => Options.Create(new WatchlistOptions
        {
            NationalIds = ["99999999990"],
            Names = ["YASAKLI TEST"]
        });
}
