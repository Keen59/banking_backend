using Banking.Contracts.Events;
using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using LedgerService.Infrastructure.Consumers;
using MassTransit;
using NSubstitute;
using Xunit;

namespace LedgerService.UnitTests;

public class AccountOpenedConsumerTests
{
    [Fact]
    public async Task Consume_opens_zero_balance_demand_deposit_projection()
    {
        var accountId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var accounts = Substitute.For<ILedgerAccountRepository>();
        accounts.ExistsBySourceAccountIdAsync(accountId, Arg.Any<CancellationToken>()).Returns(false);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.LedgerAccounts.Returns(accounts);

        var context = Substitute.For<ConsumeContext<AccountOpened>>();
        context.Message.Returns(new AccountOpened(
            accountId,
            customerId,
            "000000000001",
            "TR330010012345678901234567",
            "TRY",
            DateTimeOffset.UtcNow));
        context.CancellationToken.Returns(CancellationToken.None);

        await new AccountOpenedConsumer(unitOfWork).Consume(context);

        await accounts.Received(1).AddAsync(
            Arg.Is<LedgerAccount>(account =>
                account.SourceAccountId == accountId &&
                account.CustomerId == customerId &&
                account.Currency == "TRY" &&
                account.Kind == ELedgerAccountKind.CustomerDemandDeposit &&
                account.Status == ELedgerAccountStatus.Active),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveAsync(Arg.Any<CancellationToken>());
        await unitOfWork.JournalEntries.DidNotReceive().AddAsync(Arg.Any<JournalEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_does_not_insert_when_source_account_exists()
    {
        var accountId = Guid.NewGuid();
        var accounts = Substitute.For<ILedgerAccountRepository>();
        accounts.ExistsBySourceAccountIdAsync(accountId, Arg.Any<CancellationToken>()).Returns(true);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.LedgerAccounts.Returns(accounts);

        var context = Substitute.For<ConsumeContext<AccountOpened>>();
        context.Message.Returns(new AccountOpened(
            accountId,
            Guid.NewGuid(),
            "000000000001",
            "TR330010012345678901234567",
            "TRY",
            DateTimeOffset.UtcNow));
        context.CancellationToken.Returns(CancellationToken.None);

        await new AccountOpenedConsumer(unitOfWork).Consume(context);

        await accounts.DidNotReceive().AddAsync(Arg.Any<LedgerAccount>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }
}
