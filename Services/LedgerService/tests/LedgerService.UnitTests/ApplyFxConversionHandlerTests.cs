using Banking.Contracts.Events;
using LedgerService.Application.Commands.ApplyFxConversion;
using LedgerService.Application.Commands.ApplyTestCredit;
using LedgerService.Application.Commands.GetAccountBalance;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using Xunit;

namespace LedgerService.UnitTests;

public class ApplyFxConversionHandlerTests
{
    [Fact]
    public async Task Conversion_debits_try_and_credits_usd()
    {
        var store = SeededStore(out var tryAccount, out var usdAccount);
        var publisher = new RecordingPublisher();
        await new ApplyTestCreditHandler(store, publisher).Handle(
            new ApplyTestCreditCommand
            {
                CreditId = Guid.NewGuid(),
                AccountId = tryAccount.SourceAccountId!.Value,
                Amount = 3400m,
                Currency = "TRY"
            },
            CancellationToken.None);

        var conversionId = Guid.NewGuid();
        await new ApplyFxConversionHandler(store, publisher).Handle(
            new ApplyFxConversionCommand
            {
                ConversionId = conversionId,
                SourceAccountId = tryAccount.SourceAccountId!.Value,
                DestinationAccountId = usdAccount.SourceAccountId!.Value,
                SourceAmount = 3400m,
                SourceCurrency = "TRY",
                DestinationAmount = 100m,
                DestinationCurrency = "USD"
            },
            CancellationToken.None);

        Assert.Contains(store.Entries, x => x.IdempotencyKey == ApplyFxConversionHandler.SellJournalKey(conversionId));
        Assert.Contains(store.Entries, x => x.IdempotencyKey == ApplyFxConversionHandler.BuyJournalKey(conversionId));
        Assert.Contains(publisher.Messages, x => x is FxConversionCompleted);

        var tryBalance = await new GetAccountBalanceHandler(store).Handle(new GetAccountBalanceCommand
        {
            AccountId = tryAccount.SourceAccountId!.Value,
            RequestedCustomerId = tryAccount.CustomerId!.Value
        }, CancellationToken.None);
        var usdBalance = await new GetAccountBalanceHandler(store).Handle(new GetAccountBalanceCommand
        {
            AccountId = usdAccount.SourceAccountId!.Value,
            RequestedCustomerId = usdAccount.CustomerId!.Value
        }, CancellationToken.None);

        Assert.Equal(0m, tryBalance.Balance!.Available);
        Assert.Equal(100m, usdBalance.Balance!.Available);
    }

    [Fact]
    public async Task Conversion_same_id_does_not_double_post()
    {
        var store = SeededStore(out var tryAccount, out var usdAccount);
        var publisher = new RecordingPublisher();
        await new ApplyTestCreditHandler(store, publisher).Handle(
            new ApplyTestCreditCommand
            {
                CreditId = Guid.NewGuid(),
                AccountId = tryAccount.SourceAccountId!.Value,
                Amount = 340m,
                Currency = "TRY"
            },
            CancellationToken.None);

        var command = new ApplyFxConversionCommand
        {
            ConversionId = Guid.NewGuid(),
            SourceAccountId = tryAccount.SourceAccountId!.Value,
            DestinationAccountId = usdAccount.SourceAccountId!.Value,
            SourceAmount = 340m,
            SourceCurrency = "TRY",
            DestinationAmount = 10m,
            DestinationCurrency = "USD"
        };
        var handler = new ApplyFxConversionHandler(store, publisher);

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.SellJournalEntryId, second.SellJournalEntryId);
        Assert.Equal(2, store.Entries.Count(x =>
            x.IdempotencyKey == ApplyFxConversionHandler.SellJournalKey(command.ConversionId) ||
            x.IdempotencyKey == ApplyFxConversionHandler.BuyJournalKey(command.ConversionId)));
    }

    [Fact]
    public async Task Conversion_rejects_when_available_is_insufficient()
    {
        var store = SeededStore(out var tryAccount, out var usdAccount);
        var publisher = new RecordingPublisher();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ApplyFxConversionHandler(store, publisher).Handle(
                new ApplyFxConversionCommand
                {
                    ConversionId = Guid.NewGuid(),
                    SourceAccountId = tryAccount.SourceAccountId!.Value,
                    DestinationAccountId = usdAccount.SourceAccountId!.Value,
                    SourceAmount = 1m,
                    SourceCurrency = "TRY",
                    DestinationAmount = 0.03m,
                    DestinationCurrency = "USD"
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task Conversion_rejects_same_currency()
    {
        var store = SeededStore(out var tryAccount, out _);
        var otherTry = new LedgerAccount
        {
            Id = Guid.NewGuid(),
            SourceAccountId = Guid.NewGuid(),
            CustomerId = tryAccount.CustomerId,
            Currency = "TRY",
            Kind = ELedgerAccountKind.CustomerDemandDeposit,
            Status = ELedgerAccountStatus.Active
        };
        store.Accounts.Add(otherTry);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ApplyFxConversionHandler(store, new RecordingPublisher()).Handle(
                new ApplyFxConversionCommand
                {
                    ConversionId = Guid.NewGuid(),
                    SourceAccountId = tryAccount.SourceAccountId!.Value,
                    DestinationAccountId = otherTry.SourceAccountId!.Value,
                    SourceAmount = 10m,
                    SourceCurrency = "TRY",
                    DestinationAmount = 10m,
                    DestinationCurrency = "TRY"
                },
                CancellationToken.None));
    }

    private static InMemoryLedgerUnitOfWork SeededStore(out LedgerAccount tryAccount, out LedgerAccount usdAccount)
    {
        var store = new InMemoryLedgerUnitOfWork();
        store.Accounts.Add(new LedgerAccount
        {
            Id = SystemLedgerAccounts.InternalClearingTry,
            Currency = "TRY",
            Kind = ELedgerAccountKind.InternalClearing,
            Status = ELedgerAccountStatus.Active
        });
        store.Accounts.Add(new LedgerAccount
        {
            Id = SystemLedgerAccounts.InternalClearingUsd,
            Currency = "USD",
            Kind = ELedgerAccountKind.InternalClearing,
            Status = ELedgerAccountStatus.Active
        });
        store.Accounts.Add(new LedgerAccount
        {
            Id = SystemLedgerAccounts.InternalClearingEur,
            Currency = "EUR",
            Kind = ELedgerAccountKind.InternalClearing,
            Status = ELedgerAccountStatus.Active
        });

        var customerId = Guid.NewGuid();
        tryAccount = new LedgerAccount
        {
            Id = Guid.NewGuid(),
            SourceAccountId = Guid.NewGuid(),
            CustomerId = customerId,
            Currency = "TRY",
            Kind = ELedgerAccountKind.CustomerDemandDeposit,
            Status = ELedgerAccountStatus.Active
        };
        usdAccount = new LedgerAccount
        {
            Id = Guid.NewGuid(),
            SourceAccountId = Guid.NewGuid(),
            CustomerId = customerId,
            Currency = "USD",
            Kind = ELedgerAccountKind.CustomerDemandDeposit,
            Status = ELedgerAccountStatus.Active
        };
        store.Accounts.Add(tryAccount);
        store.Accounts.Add(usdAccount);
        return store;
    }

    private sealed class RecordingPublisher : IIntegrationEventPublisher
    {
        public List<object> Messages { get; } = [];

        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
