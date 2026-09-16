using Banking.Contracts.Events;
using LedgerService.Application.Commands.ApplyTestCredit;
using LedgerService.Application.Commands.GetAccountBalance;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using Xunit;

namespace LedgerService.UnitTests;

public class ApplyTestCreditHandlerTests
{
    [Fact]
    public async Task Handle_debits_clearing_and_credits_customer()
    {
        var store = SeededStore(out var customer);
        var publisher = new RecordingPublisher();
        var creditId = Guid.NewGuid();

        var response = await new ApplyTestCreditHandler(store, publisher).Handle(
            new ApplyTestCreditCommand
            {
                CreditId = creditId,
                AccountId = customer.SourceAccountId!.Value,
                Amount = 250m,
                Currency = "TRY"
            },
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.JournalEntryId);
        var posted = Assert.Single(publisher.Messages.OfType<TestCreditPosted>());
        Assert.Equal(creditId, posted.CreditId);

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
    public async Task Handle_same_credit_id_does_not_double_post()
    {
        var store = SeededStore(out var customer);
        var publisher = new RecordingPublisher();
        var command = new ApplyTestCreditCommand
        {
            CreditId = Guid.NewGuid(),
            AccountId = customer.SourceAccountId!.Value,
            Amount = 40m,
            Currency = "TRY"
        };
        var handler = new ApplyTestCreditHandler(store, publisher);

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.JournalEntryId, second.JournalEntryId);
        Assert.Single(store.Entries, x => x.IdempotencyKey == ApplyTestCreditHandler.JournalKey(command.CreditId));
        Assert.Equal(2, publisher.Messages.OfType<TestCreditPosted>().Count());
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
