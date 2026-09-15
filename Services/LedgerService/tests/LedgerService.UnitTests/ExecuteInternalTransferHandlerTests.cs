using Banking.Contracts.Events;
using LedgerService.Application.Commands.ExecuteInternalTransfer;
using LedgerService.Application.Commands.PostJournal;
using LedgerService.Application.DTOs.Ledger;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using Xunit;

namespace LedgerService.UnitTests;

public class ExecuteInternalTransferHandlerTests
{
    [Fact]
    public async Task Handle_posts_debit_source_credit_destination_and_releases_hold()
    {
        var store = SeededPair(out var source, out var destination);
        await Credit(store, source.Id, 200m, "credit-source");
        var publisher = new RecordingPublisher();
        var transferId = Guid.NewGuid();

        var response = await new ExecuteInternalTransferHandler(store, publisher).Handle(
            new ExecuteInternalTransferCommand
            {
                TransferId = transferId,
                SourceAccountId = source.SourceAccountId!.Value,
                DestinationAccountId = destination.SourceAccountId!.Value,
                Amount = 80m,
                Currency = "TRY",
                Description = "İç virman"
            },
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.JournalEntryId);
        Assert.Single(store.Entries, x => x.IdempotencyKey == ExecuteInternalTransferHandler.JournalKey(transferId));
        var hold = Assert.Single(store.HoldRows);
        Assert.Equal(EHoldStatus.Released, hold.Status);
        var completed = Assert.Single(publisher.Messages.OfType<TransferCompleted>());
        Assert.Equal(transferId, completed.TransferId);
        Assert.Equal(response.JournalEntryId, completed.JournalEntryId);

        var sourceLines = store.Entries.SelectMany(x => x.Lines).Where(x => x.LedgerAccountId == source.Id).ToList();
        var destLines = store.Entries.SelectMany(x => x.Lines).Where(x => x.LedgerAccountId == destination.Id).ToList();
        Assert.Contains(sourceLines, line => line.Side == EEntrySide.Debit && line.Amount == 80m);
        Assert.Contains(destLines, line => line.Side == EEntrySide.Credit && line.Amount == 80m);
    }

    [Fact]
    public async Task Handle_rejects_when_available_is_insufficient()
    {
        var store = SeededPair(out var source, out var destination);
        await Credit(store, source.Id, 10m, "credit-source");
        var publisher = new RecordingPublisher();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ExecuteInternalTransferHandler(store, publisher).Handle(
                new ExecuteInternalTransferCommand
                {
                    TransferId = Guid.NewGuid(),
                    SourceAccountId = source.SourceAccountId!.Value,
                    DestinationAccountId = destination.SourceAccountId!.Value,
                    Amount = 10.01m,
                    Currency = "TRY"
                },
                CancellationToken.None));

        Assert.DoesNotContain(store.Entries, x => x.IdempotencyKey.StartsWith("transfer:", StringComparison.Ordinal));
        Assert.Empty(store.HoldRows);
        Assert.Empty(publisher.Messages);
    }

    [Fact]
    public async Task Handle_same_transfer_id_does_not_double_post()
    {
        var store = SeededPair(out var source, out var destination);
        await Credit(store, source.Id, 100m, "credit-source");
        var publisher = new RecordingPublisher();
        var command = new ExecuteInternalTransferCommand
        {
            TransferId = Guid.NewGuid(),
            SourceAccountId = source.SourceAccountId!.Value,
            DestinationAccountId = destination.SourceAccountId!.Value,
            Amount = 40m,
            Currency = "TRY"
        };
        var handler = new ExecuteInternalTransferHandler(store, publisher);

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.JournalEntryId, second.JournalEntryId);
        Assert.Single(store.Entries, x => x.IdempotencyKey == ExecuteInternalTransferHandler.JournalKey(command.TransferId));
        Assert.Equal(2, publisher.Messages.OfType<TransferCompleted>().Count());
    }

    private static InMemoryLedgerUnitOfWork SeededPair(out LedgerAccount source, out LedgerAccount destination)
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
        destination = new LedgerAccount
        {
            Id = Guid.NewGuid(),
            SourceAccountId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Currency = "TRY",
            Kind = ELedgerAccountKind.CustomerDemandDeposit,
            Status = ELedgerAccountStatus.Active
        };
        store.Accounts.Add(source);
        store.Accounts.Add(destination);
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
