using Banking.Contracts.Events;
using LedgerService.Application.Commands.HoldEftPayment;
using LedgerService.Application.Commands.PostJournal;
using LedgerService.Application.Commands.ReturnEftPayment;
using LedgerService.Application.Commands.SettleEftPayment;
using LedgerService.Application.DTOs.Ledger;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using Xunit;

namespace LedgerService.UnitTests;

public class HoldEftPaymentHandlerTests
{
    [Fact]
    public async Task Hold_places_active_hold_and_does_not_post()
    {
        var store = SeededStore(out var source);
        await Credit(store, source.Id, 200m, "credit-source");
        var publisher = new RecordingPublisher();
        var eftId = Guid.NewGuid();

        await new HoldEftPaymentHandler(store, publisher).Handle(
            new HoldEftPaymentCommand
            {
                EftPaymentId = eftId,
                SourceAccountId = source.SourceAccountId!.Value,
                Amount = 75m,
                Currency = "TRY"
            },
            CancellationToken.None);

        var hold = Assert.Single(store.HoldRows);
        Assert.Equal(EHoldStatus.Active, hold.Status);
        Assert.Equal(HoldEftPaymentHandler.HoldKey(eftId), hold.IdempotencyKey);
        Assert.DoesNotContain(store.Entries, x => x.IdempotencyKey == HoldEftPaymentHandler.JournalKey(eftId));
        var held = Assert.Single(publisher.Messages.OfType<EftPaymentHeld>());
        Assert.Equal(eftId, held.EftPaymentId);
    }

    [Fact]
    public async Task Hold_rejects_when_available_is_insufficient()
    {
        var store = SeededStore(out var source);
        await Credit(store, source.Id, 10m, "credit-source");
        var publisher = new RecordingPublisher();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new HoldEftPaymentHandler(store, publisher).Handle(
                new HoldEftPaymentCommand
                {
                    EftPaymentId = Guid.NewGuid(),
                    SourceAccountId = source.SourceAccountId!.Value,
                    Amount = 10.01m,
                    Currency = "TRY"
                },
                CancellationToken.None));

        Assert.Empty(store.HoldRows);
        Assert.Empty(publisher.Messages);
    }

    [Fact]
    public async Task Hold_same_id_republishes_held_without_second_row()
    {
        var store = SeededStore(out var source);
        await Credit(store, source.Id, 100m, "credit-source");
        var publisher = new RecordingPublisher();
        var command = new HoldEftPaymentCommand
        {
            EftPaymentId = Guid.NewGuid(),
            SourceAccountId = source.SourceAccountId!.Value,
            Amount = 20m,
            Currency = "TRY"
        };
        var handler = new HoldEftPaymentHandler(store, publisher);

        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        Assert.Single(store.HoldRows);
        Assert.Equal(2, publisher.Messages.OfType<EftPaymentHeld>().Count());
    }

    [Fact]
    public async Task Settle_debits_source_credits_clearing_and_releases_hold()
    {
        var store = SeededStore(out var source);
        await Credit(store, source.Id, 200m, "credit-source");
        var publisher = new RecordingPublisher();
        var eftId = Guid.NewGuid();
        await new HoldEftPaymentHandler(store, publisher).Handle(
            new HoldEftPaymentCommand
            {
                EftPaymentId = eftId,
                SourceAccountId = source.SourceAccountId!.Value,
                Amount = 75m,
                Currency = "TRY"
            },
            CancellationToken.None);

        var response = await new SettleEftPaymentHandler(store, publisher).Handle(
            new SettleEftPaymentCommand { EftPaymentId = eftId },
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.JournalEntryId);
        var hold = Assert.Single(store.HoldRows);
        Assert.Equal(EHoldStatus.Released, hold.Status);
        var completed = Assert.Single(publisher.Messages.OfType<EftPaymentCompleted>());
        Assert.Equal(eftId, completed.EftPaymentId);

        Assert.Contains(
            store.Entries.SelectMany(x => x.Lines),
            line => line.LedgerAccountId == source.Id && line.Side == EEntrySide.Debit && line.Amount == 75m);
        Assert.Contains(
            store.Entries.SelectMany(x => x.Lines),
            line => line.LedgerAccountId == SystemLedgerAccounts.InternalClearingTry
                    && line.Side == EEntrySide.Credit
                    && line.Amount == 75m);
    }

    [Fact]
    public async Task Settle_same_id_does_not_double_post()
    {
        var store = SeededStore(out var source);
        await Credit(store, source.Id, 100m, "credit-source");
        var publisher = new RecordingPublisher();
        var eftId = Guid.NewGuid();
        await new HoldEftPaymentHandler(store, publisher).Handle(
            new HoldEftPaymentCommand
            {
                EftPaymentId = eftId,
                SourceAccountId = source.SourceAccountId!.Value,
                Amount = 20m,
                Currency = "TRY"
            },
            CancellationToken.None);
        var handler = new SettleEftPaymentHandler(store, publisher);

        var first = await handler.Handle(new SettleEftPaymentCommand { EftPaymentId = eftId }, CancellationToken.None);
        var second = await handler.Handle(new SettleEftPaymentCommand { EftPaymentId = eftId }, CancellationToken.None);

        Assert.Equal(first.JournalEntryId, second.JournalEntryId);
        Assert.Single(store.Entries, x => x.IdempotencyKey == HoldEftPaymentHandler.JournalKey(eftId));
        Assert.Equal(2, publisher.Messages.OfType<EftPaymentCompleted>().Count());
    }

    [Fact]
    public async Task Return_releases_hold_and_publishes_rejected()
    {
        var store = SeededStore(out var source);
        await Credit(store, source.Id, 100m, "credit-source");
        var publisher = new RecordingPublisher();
        var eftId = Guid.NewGuid();
        await new HoldEftPaymentHandler(store, publisher).Handle(
            new HoldEftPaymentCommand
            {
                EftPaymentId = eftId,
                SourceAccountId = source.SourceAccountId!.Value,
                Amount = 20m,
                Currency = "TRY"
            },
            CancellationToken.None);

        await new ReturnEftPaymentHandler(store, publisher).Handle(
            new ReturnEftPaymentCommand { EftPaymentId = eftId, Reason = "iban mismatch" },
            CancellationToken.None);

        var hold = Assert.Single(store.HoldRows);
        Assert.Equal(EHoldStatus.Released, hold.Status);
        Assert.DoesNotContain(store.Entries, x => x.IdempotencyKey == HoldEftPaymentHandler.JournalKey(eftId));
        var rejected = Assert.Single(publisher.Messages.OfType<EftPaymentRejected>());
        Assert.Equal("iban mismatch", rejected.Reason);
    }

    [Fact]
    public async Task Return_after_settle_throws()
    {
        var store = SeededStore(out var source);
        await Credit(store, source.Id, 100m, "credit-source");
        var publisher = new RecordingPublisher();
        var eftId = Guid.NewGuid();
        await new HoldEftPaymentHandler(store, publisher).Handle(
            new HoldEftPaymentCommand
            {
                EftPaymentId = eftId,
                SourceAccountId = source.SourceAccountId!.Value,
                Amount = 20m,
                Currency = "TRY"
            },
            CancellationToken.None);
        await new SettleEftPaymentHandler(store, publisher).Handle(
            new SettleEftPaymentCommand { EftPaymentId = eftId },
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ReturnEftPaymentHandler(store, publisher).Handle(
                new ReturnEftPaymentCommand { EftPaymentId = eftId },
                CancellationToken.None));

        Assert.Contains("takas", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Settle_after_return_throws()
    {
        var store = SeededStore(out var source);
        await Credit(store, source.Id, 100m, "credit-source");
        var publisher = new RecordingPublisher();
        var eftId = Guid.NewGuid();
        await new HoldEftPaymentHandler(store, publisher).Handle(
            new HoldEftPaymentCommand
            {
                EftPaymentId = eftId,
                SourceAccountId = source.SourceAccountId!.Value,
                Amount = 20m,
                Currency = "TRY"
            },
            CancellationToken.None);
        await new ReturnEftPaymentHandler(store, publisher).Handle(
            new ReturnEftPaymentCommand { EftPaymentId = eftId },
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SettleEftPaymentHandler(store, publisher).Handle(
                new SettleEftPaymentCommand { EftPaymentId = eftId },
                CancellationToken.None));

        Assert.Contains("iade", exception.Message, StringComparison.OrdinalIgnoreCase);
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
