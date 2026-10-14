using Banking.Contracts.Events;
using LedgerService.Application.Commands.ApplyLoanDisbursement;
using LedgerService.Application.Commands.ApplyLoanRepayment;
using LedgerService.Application.Commands.GetAccountBalance;
using LedgerService.Application.Interfaces.Services;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using Xunit;

namespace LedgerService.UnitTests;

public class ApplyLoanDisbursementHandlerTests
{
    [Fact]
    public async Task Disbursement_debits_clearing_and_credits_customer()
    {
        var store = SeededStore(out var customer);
        var publisher = new RecordingPublisher();
        var loanId = Guid.NewGuid();

        await new ApplyLoanDisbursementHandler(store, publisher).Handle(
            new ApplyLoanDisbursementCommand
            {
                LoanId = loanId,
                AccountId = customer.SourceAccountId!.Value,
                Amount = 1200m,
                Currency = "TRY"
            },
            CancellationToken.None);

        var posted = Assert.Single(publisher.Messages.OfType<LoanDisbursementPosted>());
        Assert.Equal(loanId, posted.LoanId);
        Assert.Contains(store.Entries, x => x.IdempotencyKey == ApplyLoanDisbursementHandler.JournalKey(loanId));

        var balance = await new GetAccountBalanceHandler(store).Handle(new GetAccountBalanceCommand
        {
            AccountId = customer.SourceAccountId!.Value,
            RequestedCustomerId = customer.CustomerId!.Value
        }, CancellationToken.None);

        Assert.Equal(1200m, balance.Balance!.Available);
    }

    [Fact]
    public async Task Disbursement_same_loan_id_does_not_double_post()
    {
        var store = SeededStore(out var customer);
        var publisher = new RecordingPublisher();
        var command = new ApplyLoanDisbursementCommand
        {
            LoanId = Guid.NewGuid(),
            AccountId = customer.SourceAccountId!.Value,
            Amount = 100m,
            Currency = "TRY"
        };
        var handler = new ApplyLoanDisbursementHandler(store, publisher);

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.JournalEntryId, second.JournalEntryId);
        Assert.Single(store.Entries, x => x.IdempotencyKey == ApplyLoanDisbursementHandler.JournalKey(command.LoanId));
    }

    [Fact]
    public async Task Repayment_debits_customer_and_credits_clearing()
    {
        var store = SeededStore(out var customer);
        var publisher = new RecordingPublisher();
        var loanId = Guid.NewGuid();
        await new ApplyLoanDisbursementHandler(store, publisher).Handle(
            new ApplyLoanDisbursementCommand
            {
                LoanId = loanId,
                AccountId = customer.SourceAccountId!.Value,
                Amount = 300m,
                Currency = "TRY"
            },
            CancellationToken.None);

        var installmentId = Guid.NewGuid();
        await new ApplyLoanRepaymentHandler(store, publisher).Handle(
            new ApplyLoanRepaymentCommand
            {
                LoanId = loanId,
                InstallmentId = installmentId,
                AccountId = customer.SourceAccountId!.Value,
                Amount = 100m,
                Currency = "TRY"
            },
            CancellationToken.None);

        Assert.Contains(publisher.Messages, x => x is LoanRepaymentPosted);
        var balance = await new GetAccountBalanceHandler(store).Handle(new GetAccountBalanceCommand
        {
            AccountId = customer.SourceAccountId!.Value,
            RequestedCustomerId = customer.CustomerId!.Value
        }, CancellationToken.None);
        Assert.Equal(200m, balance.Balance!.Available);
    }

    [Fact]
    public async Task Repayment_rejects_when_available_is_insufficient()
    {
        var store = SeededStore(out var customer);
        var publisher = new RecordingPublisher();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ApplyLoanRepaymentHandler(store, publisher).Handle(
                new ApplyLoanRepaymentCommand
                {
                    LoanId = Guid.NewGuid(),
                    InstallmentId = Guid.NewGuid(),
                    AccountId = customer.SourceAccountId!.Value,
                    Amount = 1m,
                    Currency = "TRY"
                },
                CancellationToken.None));
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
