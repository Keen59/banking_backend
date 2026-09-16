using Banking.Contracts.Events;
using LedgerService.Application.Commands.ApplyIncomingFastPayment;
using LedgerService.Application.Commands.GetAccountBalance;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using Xunit;

namespace LedgerService.UnitTests;

public class ApplyIncomingFastPaymentHandlerTests
{
    [Fact]
    public async Task Handle_debits_clearing_credits_customer_and_does_not_hold()
    {
        var store = SeededStore(out var customer);
        var publisher = new RecordingPublisher();
        var incomingId = Guid.NewGuid();

        var response = await new ApplyIncomingFastPaymentHandler(store, publisher).Handle(
            new ApplyIncomingFastPaymentCommand
            {
                IncomingFastPaymentId = incomingId,
                DestinationAccountId = customer.SourceAccountId!.Value,
                Amount = 250m,
                Currency = "TRY",
                Description = "Gelen FAST"
            },
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.JournalEntryId);
        Assert.Empty(store.HoldRows);
        var completed = Assert.Single(publisher.Messages.OfType<IncomingFastPaymentCompleted>());
        Assert.Equal(incomingId, completed.IncomingFastPaymentId);

        Assert.Contains(
            store.Entries.SelectMany(x => x.Lines),
            line => line.LedgerAccountId == SystemLedgerAccounts.InternalClearingTry
                    && line.Side == EEntrySide.Debit
                    && line.Amount == 250m);
        Assert.Contains(
            store.Entries.SelectMany(x => x.Lines),
            line => line.LedgerAccountId == customer.Id
                    && line.Side == EEntrySide.Credit
                    && line.Amount == 250m);

        var balance = await new GetAccountBalanceHandler(store).Handle(new GetAccountBalanceCommand
        {
            AccountId = customer.SourceAccountId!.Value,
            RequestedCustomerId = customer.CustomerId!.Value
        }, CancellationToken.None);

        Assert.Equal(250m, balance.Balance!.Ledger);
        Assert.Equal(0m, balance.Balance.Hold);
        Assert.Equal(250m, balance.Balance.Available);
    }

    [Fact]
    public async Task Handle_same_id_does_not_double_post()
    {
        var store = SeededStore(out var customer);
        var publisher = new RecordingPublisher();
        var command = new ApplyIncomingFastPaymentCommand
        {
            IncomingFastPaymentId = Guid.NewGuid(),
            DestinationAccountId = customer.SourceAccountId!.Value,
            Amount = 40m,
            Currency = "TRY"
        };
        var handler = new ApplyIncomingFastPaymentHandler(store, publisher);

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.JournalEntryId, second.JournalEntryId);
        Assert.Single(store.Entries, x => x.IdempotencyKey == ApplyIncomingFastPaymentHandler.JournalKey(command.IncomingFastPaymentId));
        Assert.Equal(2, publisher.Messages.OfType<IncomingFastPaymentCompleted>().Count());
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
