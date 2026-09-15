using AccountService.Application.Interfaces.Repositories;
using AccountService.Application.Interfaces.Services;
using AccountService.Domain.Entities;
using AccountService.Domain.Enums;
using AccountService.Infrastructure.Consumers;
using Banking.Contracts.Events;
using MassTransit;
using NSubstitute;
using Xunit;

namespace AccountService.UnitTests;

public class KycApprovedConsumerTests
{
    [Fact]
    public async Task Consume_opens_demand_deposit_try_account_when_missing()
    {
        var customerId = Guid.NewGuid();
        var accounts = Substitute.For<IAccountRepository>();
        accounts.ExistsAsync(
                customerId,
                EAccountProductType.DemandDeposit,
                "TRY",
                Arg.Any<CancellationToken>())
            .Returns(false);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.AccountRepository.Returns(accounts);

        var iban = Substitute.For<IIbanService>();
        iban.GenerateAsync(Arg.Any<CancellationToken>()).Returns(("TR330010012345678901234567", "1234567890123456"));

        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var context = Substitute.For<ConsumeContext<KycApproved>>();
        context.Message.Returns(new KycApproved(customerId, "000000000001", DateTimeOffset.UtcNow));
        context.CancellationToken.Returns(CancellationToken.None);

        var consumer = new KycApprovedConsumer(unitOfWork, iban, publisher);
        await consumer.Consume(context);

        await accounts.Received(1).AddAsync(
            Arg.Is<Account>(account =>
                account.CustomerId == customerId &&
                account.CifNumber == "000000000001" &&
                account.Iban == "TR330010012345678901234567" &&
                account.AccountNumber == "1234567890123456" &&
                account.ProductType == EAccountProductType.DemandDeposit &&
                account.Status == EAccountStatus.Active &&
                account.Currency == "TRY"),
            Arg.Any<CancellationToken>());
        await publisher.Received(1).PublishAsync(
            Arg.Is<AccountOpened>(opened =>
                opened.CustomerId == customerId &&
                opened.CifNumber == "000000000001" &&
                opened.Iban == "TR330010012345678901234567" &&
                opened.Currency == "TRY"),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_does_not_open_second_demand_deposit_try_account()
    {
        var customerId = Guid.NewGuid();
        var accounts = Substitute.For<IAccountRepository>();
        accounts.ExistsAsync(
                customerId,
                EAccountProductType.DemandDeposit,
                "TRY",
                Arg.Any<CancellationToken>())
            .Returns(true);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.AccountRepository.Returns(accounts);

        var iban = Substitute.For<IIbanService>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var context = Substitute.For<ConsumeContext<KycApproved>>();
        context.Message.Returns(new KycApproved(customerId, "000000000001", DateTimeOffset.UtcNow));
        context.CancellationToken.Returns(CancellationToken.None);

        var consumer = new KycApprovedConsumer(unitOfWork, iban, publisher);
        await consumer.Consume(context);

        await iban.DidNotReceive().GenerateAsync(Arg.Any<CancellationToken>());
        await accounts.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
        await publisher.DidNotReceive().PublishAsync(Arg.Any<AccountOpened>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }
}
