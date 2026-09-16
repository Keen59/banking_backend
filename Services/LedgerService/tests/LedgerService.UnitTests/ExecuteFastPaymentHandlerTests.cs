using Banking.Contracts.Events;
using LedgerService.Application.Commands.ExecuteFastPayment;
using LedgerService.Application.Commands.PostJournal;
using LedgerService.Application.DTOs.Ledger;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using Xunit;

namespace LedgerService.UnitTests;

public class ExecuteFastPaymentHandlerTests
{
    [Fact]
    public async Task Handle_debits_source_credits_clearing_and_releases_hold()
    {
        var store = SeededStore(out var source);
        await Credit(store, source.Id, 200m, "credit-source");
        var publisher = new RecordingPublisher();
        var fastId = Guid.NewGuid();

        var response = await new ExecuteFastPaymentHandler(store, publisher).Handle(
            new ExecuteFastPaymentCommand
            {
                FastPaymentId = fastId,
                SourceAccountId = source.SourceAccountId!.Value,
                DestinationIban = "TR330000129999999999999999",
                Amount = 75m,
                Currency = "TRY",
                Description = "FAST"
            },
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.JournalEntryId);
        var hold = Assert.Single(store.HoldRows);
        Assert.Equal(EHoldStatus.Released, hold.Status);
        var completed = Assert.Single(publisher.Messages.OfType<FastPaymentCompleted>());
        Assert.Equal(fastId, completed.FastPaymentId);

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
    public async Task Handle_rejects_when_available_is_insufficient()
    {
        var store = SeededStore(out var source);
        await Credit(store, source.Id, 10m, "credit-source");
        var publisher = new RecordingPublisher();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ExecuteFastPaymentHandler(store, publisher).Handle(
                new ExecuteFastPaymentCommand
                {
                    FastPaymentId = Guid.NewGuid(),
                    SourceAccountId = source.SourceAccountId!.Value,
                    DestinationIban = "TR33",
                    Amount = 10.01m,
                    Currency = "TRY"
                },
                CancellationToken.None));

        Assert.DoesNotContain(store.Entries, x => x.IdempotencyKey.StartsWith("fast:", StringComparison.Ordinal));
        Assert.Empty(store.HoldRows);
        Assert.Empty(publisher.Messages);
    }

    [Fact]
    public async Task Handle_same_fast_id_does_not_double_post()
    {
        var store = SeededStore(out var source);
        await Credit(store, source.Id, 100m, "credit-source");
        var publisher = new RecordingPublisher();
        var command = new ExecuteFastPaymentCommand
        {
            FastPaymentId = Guid.NewGuid(),
            SourceAccountId = source.SourceAccountId!.Value,
            DestinationIban = "TR33",
            Amount = 20m,
            Currency = "TRY"
        };
        var handler = new ExecuteFastPaymentHandler(store, publisher);

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.JournalEntryId, second.JournalEntryId);
        Assert.Single(store.Entries, x => x.IdempotencyKey == ExecuteFastPaymentHandler.JournalKey(command.FastPaymentId));
        Assert.Equal(2, publisher.Messages.OfType<FastPaymentCompleted>().Count());
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
