using Banking.Contracts.Events;
using LedgerService.Application.Commands.AcceptIncomingEftPayment;
using LedgerService.Application.Commands.GetAccountBalance;
using LedgerService.Application.Commands.ReturnIncomingEftPayment;
using LedgerService.Application.Commands.SettleIncomingEftPayment;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using Xunit;

namespace LedgerService.UnitTests;

public class AcceptIncomingEftPaymentHandlerTests
{
    [Fact]
    public async Task Accept_holds_clearing_and_does_not_credit_customer()
    {
        var store = SeededStore(out var customer);
        var publisher = new RecordingPublisher();
        var incomingId = Guid.NewGuid();

        await new AcceptIncomingEftPaymentHandler(store, publisher).Handle(
            new AcceptIncomingEftPaymentCommand
            {
                IncomingEftPaymentId = incomingId,
                DestinationAccountId = customer.SourceAccountId!.Value,
                Amount = 75m,
                Currency = "TRY"
            },
            CancellationToken.None);

        var hold = Assert.Single(store.HoldRows);
        Assert.Equal(EHoldStatus.Active, hold.Status);
        Assert.Equal(SystemLedgerAccounts.InternalClearingTry, hold.LedgerAccountId);
        Assert.DoesNotContain(store.Entries, x => x.IdempotencyKey == AcceptIncomingEftPaymentHandler.JournalKey(incomingId));
        var held = Assert.Single(publisher.Messages.OfType<IncomingEftPaymentHeld>());
        Assert.Equal(incomingId, held.IncomingEftPaymentId);

        var balance = await new GetAccountBalanceHandler(store).Handle(new GetAccountBalanceCommand
        {
            AccountId = customer.SourceAccountId!.Value,
            RequestedCustomerId = customer.CustomerId!.Value
        }, CancellationToken.None);

        Assert.Equal(0m, balance.Balance!.Ledger);
        Assert.Equal(0m, balance.Balance.Hold);
        Assert.Equal(0m, balance.Balance.Available);
    }

    [Fact]
    public async Task Accept_same_id_republishes_held_without_second_row()
    {
        var store = SeededStore(out var customer);
        var publisher = new RecordingPublisher();
        var command = new AcceptIncomingEftPaymentCommand
        {
            IncomingEftPaymentId = Guid.NewGuid(),
            DestinationAccountId = customer.SourceAccountId!.Value,
            Amount = 20m,
            Currency = "TRY"
        };
        var handler = new AcceptIncomingEftPaymentHandler(store, publisher);

        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        Assert.Single(store.HoldRows);
        Assert.Equal(2, publisher.Messages.OfType<IncomingEftPaymentHeld>().Count());
    }

    [Fact]
    public async Task Settle_debits_clearing_credits_customer_and_releases_hold()
    {
        var store = SeededStore(out var customer);
        var publisher = new RecordingPublisher();
        var incomingId = Guid.NewGuid();
        await new AcceptIncomingEftPaymentHandler(store, publisher).Handle(
            new AcceptIncomingEftPaymentCommand
            {
                IncomingEftPaymentId = incomingId,
                DestinationAccountId = customer.SourceAccountId!.Value,
                Amount = 75m,
                Currency = "TRY"
            },
            CancellationToken.None);

        var response = await new SettleIncomingEftPaymentHandler(store, publisher).Handle(
            new SettleIncomingEftPaymentCommand
            {
                IncomingEftPaymentId = incomingId,
                DestinationAccountId = customer.SourceAccountId!.Value
            },
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.JournalEntryId);
        var hold = Assert.Single(store.HoldRows);
        Assert.Equal(EHoldStatus.Released, hold.Status);
        Assert.Single(publisher.Messages.OfType<IncomingEftPaymentCompleted>());

        var balance = await new GetAccountBalanceHandler(store).Handle(new GetAccountBalanceCommand
        {
            AccountId = customer.SourceAccountId!.Value,
            RequestedCustomerId = customer.CustomerId!.Value
        }, CancellationToken.None);

        Assert.Equal(75m, balance.Balance!.Ledger);
        Assert.Equal(0m, balance.Balance.Hold);
        Assert.Equal(75m, balance.Balance.Available);
    }

    [Fact]
    public async Task Settle_same_id_does_not_double_post()
    {
        var store = SeededStore(out var customer);
        var publisher = new RecordingPublisher();
        var incomingId = Guid.NewGuid();
        await new AcceptIncomingEftPaymentHandler(store, publisher).Handle(
            new AcceptIncomingEftPaymentCommand
            {
                IncomingEftPaymentId = incomingId,
                DestinationAccountId = customer.SourceAccountId!.Value,
                Amount = 20m,
                Currency = "TRY"
            },
            CancellationToken.None);
        var handler = new SettleIncomingEftPaymentHandler(store, publisher);
        var command = new SettleIncomingEftPaymentCommand
        {
            IncomingEftPaymentId = incomingId,
            DestinationAccountId = customer.SourceAccountId!.Value
        };

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.JournalEntryId, second.JournalEntryId);
        Assert.Single(store.Entries, x => x.IdempotencyKey == AcceptIncomingEftPaymentHandler.JournalKey(incomingId));
        Assert.Equal(2, publisher.Messages.OfType<IncomingEftPaymentCompleted>().Count());
    }

    [Fact]
    public async Task Return_releases_hold_and_does_not_post()
    {
        var store = SeededStore(out var customer);
        var publisher = new RecordingPublisher();
        var incomingId = Guid.NewGuid();
        await new AcceptIncomingEftPaymentHandler(store, publisher).Handle(
            new AcceptIncomingEftPaymentCommand
            {
                IncomingEftPaymentId = incomingId,
                DestinationAccountId = customer.SourceAccountId!.Value,
                Amount = 20m,
                Currency = "TRY"
            },
            CancellationToken.None);

        await new ReturnIncomingEftPaymentHandler(store, publisher).Handle(
            new ReturnIncomingEftPaymentCommand { IncomingEftPaymentId = incomingId, Reason = "iban mismatch" },
            CancellationToken.None);

        var hold = Assert.Single(store.HoldRows);
        Assert.Equal(EHoldStatus.Released, hold.Status);
        Assert.DoesNotContain(store.Entries, x => x.IdempotencyKey == AcceptIncomingEftPaymentHandler.JournalKey(incomingId));
        var rejected = Assert.Single(publisher.Messages.OfType<IncomingEftPaymentRejected>());
        Assert.Equal("iban mismatch", rejected.Reason);
    }

    [Fact]
    public async Task Return_after_settle_throws()
    {
        var store = SeededStore(out var customer);
        var publisher = new RecordingPublisher();
        var incomingId = Guid.NewGuid();
        await new AcceptIncomingEftPaymentHandler(store, publisher).Handle(
            new AcceptIncomingEftPaymentCommand
            {
                IncomingEftPaymentId = incomingId,
                DestinationAccountId = customer.SourceAccountId!.Value,
                Amount = 20m,
                Currency = "TRY"
            },
            CancellationToken.None);
        await new SettleIncomingEftPaymentHandler(store, publisher).Handle(
            new SettleIncomingEftPaymentCommand
            {
                IncomingEftPaymentId = incomingId,
                DestinationAccountId = customer.SourceAccountId!.Value
            },
            CancellationToken.None);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ReturnIncomingEftPaymentHandler(store, publisher).Handle(
                new ReturnIncomingEftPaymentCommand { IncomingEftPaymentId = incomingId },
                CancellationToken.None));

        Assert.Contains("takas", exception.Message, StringComparison.OrdinalIgnoreCase);
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
