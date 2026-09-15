using LedgerService.Application.Commands.GetAccountBalance;
using LedgerService.Application.Commands.PlaceHold;
using LedgerService.Application.Commands.PostJournal;
using LedgerService.Application.Commands.ReleaseHold;
using LedgerService.Application.DTOs.Ledger;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using Xunit;

namespace LedgerService.UnitTests;

public class PostingHandlerTests
{
    [Fact]
    public async Task PostJournal_credits_customer_liability_and_debits_clearing()
    {
        var store = SeededStore(out var customerAccount);
        var handler = new PostJournalHandler(store);

        await handler.Handle(new PostJournalCommand
        {
            IdempotencyKey = "post-1",
            Description = "Opening test credit",
            Lines =
            [
                new JournalLineInput
                {
                    LedgerAccountId = SystemLedgerAccounts.InternalClearingTry,
                    Side = EEntrySide.Debit,
                    Amount = 100m
                },
                new JournalLineInput
                {
                    LedgerAccountId = customerAccount.Id,
                    Side = EEntrySide.Credit,
                    Amount = 100m
                }
            ]
        }, CancellationToken.None);

        var balance = await new GetAccountBalanceHandler(store).Handle(new GetAccountBalanceCommand
        {
            AccountId = customerAccount.SourceAccountId!.Value,
            RequestedCustomerId = customerAccount.CustomerId!.Value
        }, CancellationToken.None);

        Assert.Equal(100m, balance.Balance!.Ledger);
        Assert.Equal(0m, balance.Balance.Hold);
        Assert.Equal(100m, balance.Balance.Available);
    }

    [Fact]
    public async Task PostJournal_rejects_unbalanced_lines()
    {
        var store = SeededStore(out var customerAccount);
        var handler = new PostJournalHandler(store);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new PostJournalCommand
            {
                IdempotencyKey = "unbalanced",
                Description = "bad",
                Lines =
                [
                    new JournalLineInput
                    {
                        LedgerAccountId = SystemLedgerAccounts.InternalClearingTry,
                        Side = EEntrySide.Debit,
                        Amount = 50m
                    },
                    new JournalLineInput
                    {
                        LedgerAccountId = customerAccount.Id,
                        Side = EEntrySide.Credit,
                        Amount = 40m
                    }
                ]
            }, CancellationToken.None));

        Assert.Contains("eşit", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(store.Entries);
    }

    [Fact]
    public async Task PostJournal_same_idempotency_key_does_not_insert_twice()
    {
        var store = SeededStore(out var customerAccount);
        var handler = new PostJournalHandler(store);
        var command = BalancedCredit(customerAccount.Id, "dup-key", 25m);

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.JournalEntryId, second.JournalEntryId);
        Assert.Single(store.Entries);
    }

    [Fact]
    public async Task PlaceHold_rejects_when_available_is_insufficient()
    {
        var store = SeededStore(out var customerAccount);
        await new PostJournalHandler(store).Handle(
            BalancedCredit(customerAccount.Id, "credit-10", 10m),
            CancellationToken.None);

        var handler = new PlaceHoldHandler(store);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new PlaceHoldCommand
            {
                LedgerAccountId = customerAccount.Id,
                IdempotencyKey = "hold-too-big",
                Amount = 10.01m
            }, CancellationToken.None));
    }

    [Fact]
    public async Task ReleaseHold_restores_available_without_changing_ledger()
    {
        var store = SeededStore(out var customerAccount);
        await new PostJournalHandler(store).Handle(
            BalancedCredit(customerAccount.Id, "credit-50", 50m),
            CancellationToken.None);

        var place = await new PlaceHoldHandler(store).Handle(new PlaceHoldCommand
        {
            LedgerAccountId = customerAccount.Id,
            IdempotencyKey = "hold-20",
            Amount = 20m
        }, CancellationToken.None);

        Assert.Equal(50m, place.Balance!.Ledger);
        Assert.Equal(20m, place.Balance.Hold);
        Assert.Equal(30m, place.Balance.Available);

        var released = await new ReleaseHoldHandler(store).Handle(new ReleaseHoldCommand
        {
            IdempotencyKey = "hold-20"
        }, CancellationToken.None);

        Assert.Equal(50m, released.Balance!.Ledger);
        Assert.Equal(0m, released.Balance.Hold);
        Assert.Equal(50m, released.Balance.Available);
    }

    private static InMemoryLedgerUnitOfWork SeededStore(out LedgerAccount customerAccount)
    {
        var store = new InMemoryLedgerUnitOfWork();
        store.Accounts.Add(new LedgerAccount
        {
            Id = SystemLedgerAccounts.InternalClearingTry,
            Currency = "TRY",
            Kind = ELedgerAccountKind.InternalClearing,
            Status = ELedgerAccountStatus.Active
        });

        customerAccount = new LedgerAccount
        {
            Id = Guid.NewGuid(),
            SourceAccountId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Currency = "TRY",
            Kind = ELedgerAccountKind.CustomerDemandDeposit,
            Status = ELedgerAccountStatus.Active
        };
        store.Accounts.Add(customerAccount);
        return store;
    }

    private static PostJournalCommand BalancedCredit(Guid customerLedgerAccountId, string key, decimal amount)
        => new()
        {
            IdempotencyKey = key,
            Description = "test credit",
            Lines =
            [
                new JournalLineInput
                {
                    LedgerAccountId = SystemLedgerAccounts.InternalClearingTry,
                    Side = EEntrySide.Debit,
                    Amount = amount
                },
                new JournalLineInput
                {
                    LedgerAccountId = customerLedgerAccountId,
                    Side = EEntrySide.Credit,
                    Amount = amount
                }
            ]
        };
}
