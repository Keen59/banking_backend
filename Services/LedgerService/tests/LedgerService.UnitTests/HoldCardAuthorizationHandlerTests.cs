using Banking.Contracts.Events;
using LedgerService.Application.Commands.CaptureCardAuthorization;
using LedgerService.Application.Commands.HoldCardAuthorization;
using LedgerService.Application.Commands.PostJournal;
using LedgerService.Application.Commands.ReverseCardAuthorization;
using LedgerService.Application.DTOs.Ledger;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using Xunit;

namespace LedgerService.UnitTests;

public class HoldCardAuthorizationHandlerTests
{
    [Fact]
    public async Task Hold_places_active_hold_and_does_not_post()
    {
        var store = SeededStore(out var source);
        await Credit(store, source.Id, 200m, "credit-source");
        var publisher = new RecordingPublisher();
        var authId = Guid.NewGuid();

        await new HoldCardAuthorizationHandler(store, publisher).Handle(
            new HoldCardAuthorizationCommand
            {
                AuthorizationId = authId,
                AccountId = source.SourceAccountId!.Value,
                Amount = 75m,
                Currency = "TRY",
                Merchant = "POS"
            },
            CancellationToken.None);

        var hold = Assert.Single(store.HoldRows);
        Assert.Equal(EHoldStatus.Active, hold.Status);
        Assert.Equal(HoldCardAuthorizationHandler.HoldKey(authId), hold.IdempotencyKey);
        Assert.DoesNotContain(store.Entries, x => x.IdempotencyKey == HoldCardAuthorizationHandler.JournalKey(authId));
        var held = Assert.Single(publisher.Messages.OfType<CardAuthorizationHeld>());
        Assert.Equal(authId, held.AuthorizationId);
    }

    [Fact]
    public async Task Capture_debits_customer_credits_clearing_and_releases_hold()
    {
        var store = SeededStore(out var source);
        await Credit(store, source.Id, 200m, "credit-source");
        var publisher = new RecordingPublisher();
        var authId = Guid.NewGuid();
        await new HoldCardAuthorizationHandler(store, publisher).Handle(
            new HoldCardAuthorizationCommand
            {
                AuthorizationId = authId,
                AccountId = source.SourceAccountId!.Value,
                Amount = 50m,
                Currency = "TRY",
                Merchant = "POS"
            },
            CancellationToken.None);

        await new CaptureCardAuthorizationHandler(store, publisher).Handle(
            new CaptureCardAuthorizationCommand { AuthorizationId = authId, Description = "POS capture" },
            CancellationToken.None);

        Assert.Equal(EHoldStatus.Released, Assert.Single(store.HoldRows).Status);
        Assert.Contains(store.Entries, x => x.IdempotencyKey == HoldCardAuthorizationHandler.JournalKey(authId));
        Assert.Contains(publisher.Messages, x => x is CardAuthorizationCompleted);
    }

    [Fact]
    public async Task Reverse_releases_hold_without_journal()
    {
        var store = SeededStore(out var source);
        await Credit(store, source.Id, 200m, "credit-source");
        var publisher = new RecordingPublisher();
        var authId = Guid.NewGuid();
        await new HoldCardAuthorizationHandler(store, publisher).Handle(
            new HoldCardAuthorizationCommand
            {
                AuthorizationId = authId,
                AccountId = source.SourceAccountId!.Value,
                Amount = 50m,
                Currency = "TRY",
                Merchant = "POS"
            },
            CancellationToken.None);

        await new ReverseCardAuthorizationHandler(store, publisher).Handle(
            new ReverseCardAuthorizationCommand { AuthorizationId = authId, Reason = "void" },
            CancellationToken.None);

        Assert.Equal(EHoldStatus.Released, Assert.Single(store.HoldRows).Status);
        Assert.DoesNotContain(store.Entries, x => x.IdempotencyKey == HoldCardAuthorizationHandler.JournalKey(authId));
        var rejected = Assert.Single(publisher.Messages.OfType<CardAuthorizationRejected>());
        Assert.Equal("void", rejected.Reason);
    }

    private static InMemoryLedgerUnitOfWork SeededStore(out LedgerAccount source)
    {
        var store = new InMemoryLedgerUnitOfWork();
        store.Accounts.Add(new LedgerAccount
        {
            Id = SystemLedgerAccounts.InternalClearingTry,
            Currency = "TRY",
            Kind = ELedgerAccountKind.InternalClearing,
            Status = ELedgerAccountStatus.Active
        });

        source = new LedgerAccount
        {
            Id = Guid.NewGuid(),
            SourceAccountId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Currency = "TRY",
            Kind = ELedgerAccountKind.CustomerDemandDeposit,
            Status = ELedgerAccountStatus.Active
        };
        store.Accounts.Add(source);
        return store;
    }

    private static Task Credit(InMemoryLedgerUnitOfWork store, Guid customerLedgerAccountId, decimal amount, string key)
        => new PostJournalHandler(store).Handle(new PostJournalCommand
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
        }, CancellationToken.None);

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
