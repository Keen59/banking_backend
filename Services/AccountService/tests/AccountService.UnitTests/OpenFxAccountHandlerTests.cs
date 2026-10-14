using AccountService.Application.Commands.OpenFxAccount;
using AccountService.Application.Interfaces.Repositories;
using AccountService.Application.Interfaces.Services;
using AccountService.Domain.Entities;
using AccountService.Domain.Enums;
using Banking.Contracts.Events;
using NSubstitute;
using Xunit;

namespace AccountService.UnitTests;

public class OpenFxAccountHandlerTests
{
    [Fact]
    public async Task Handle_opens_usd_account_when_try_exists()
    {
        var customerId = Guid.NewGuid();
        var tryAccount = TryAccount(customerId);
        var accounts = Substitute.For<IAccountRepository>();
        accounts.GetByCustomerIdAsync(customerId, Arg.Any<CancellationToken>())
            .Returns([tryAccount]);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.AccountRepository.Returns(accounts);

        var iban = Substitute.For<IIbanService>();
        iban.GenerateAsync(Arg.Any<CancellationToken>()).Returns(("TR330010098765432109876543", "9876543210987654"));

        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var handler = new OpenFxAccountHandler(unitOfWork, iban, publisher);

        var response = await handler.Handle(new OpenFxAccountCommand
        {
            CustomerId = customerId,
            Currency = "usd"
        }, CancellationToken.None);

        Assert.Equal("USD", response.Account!.Currency);
        Assert.Equal(tryAccount.CifNumber, response.Account.CifNumber);
        await publisher.Received(1).PublishAsync(
            Arg.Is<AccountOpened>(opened =>
                opened.CustomerId == customerId &&
                opened.Currency == "USD" &&
                opened.Iban == "TR330010098765432109876543"),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_returns_existing_usd_without_second_publish()
    {
        var customerId = Guid.NewGuid();
        var usd = new Account
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CifNumber = "000000000001",
            Iban = "TR330010012345678901234567",
            AccountNumber = "1234567890123456",
            ProductType = EAccountProductType.DemandDeposit,
            Status = EAccountStatus.Active,
            Currency = "USD"
        };
        var accounts = Substitute.For<IAccountRepository>();
        accounts.GetByCustomerIdAsync(customerId, Arg.Any<CancellationToken>())
            .Returns([TryAccount(customerId), usd]);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.AccountRepository.Returns(accounts);
        var iban = Substitute.For<IIbanService>();
        var publisher = Substitute.For<IIntegrationEventPublisher>();
        var handler = new OpenFxAccountHandler(unitOfWork, iban, publisher);

        var response = await handler.Handle(new OpenFxAccountCommand
        {
            CustomerId = customerId,
            Currency = "USD"
        }, CancellationToken.None);

        Assert.Equal(usd.Id, response.Account!.Id);
        await iban.DidNotReceive().GenerateAsync(Arg.Any<CancellationToken>());
        await publisher.DidNotReceive().PublishAsync(Arg.Any<AccountOpened>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_try_on_this_endpoint()
    {
        var handler = new OpenFxAccountHandler(
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IIbanService>(),
            Substitute.For<IIntegrationEventPublisher>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new OpenFxAccountCommand
            {
                CustomerId = Guid.NewGuid(),
                Currency = "TRY"
            }, CancellationToken.None));

        Assert.Contains("USD, EUR", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Handle_rejects_when_try_account_missing()
    {
        var customerId = Guid.NewGuid();
        var accounts = Substitute.For<IAccountRepository>();
        accounts.GetByCustomerIdAsync(customerId, Arg.Any<CancellationToken>())
            .Returns([]);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.AccountRepository.Returns(accounts);
        var handler = new OpenFxAccountHandler(
            unitOfWork,
            Substitute.For<IIbanService>(),
            Substitute.For<IIntegrationEventPublisher>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new OpenFxAccountCommand
            {
                CustomerId = customerId,
                Currency = "EUR"
            }, CancellationToken.None));

        Assert.Contains("TRY", exception.Message, StringComparison.Ordinal);
    }

    private static Account TryAccount(Guid customerId) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        CifNumber = "000000000001",
        Iban = "TR330010012345678901234567",
        AccountNumber = "1234567890123456",
        ProductType = EAccountProductType.DemandDeposit,
        Status = EAccountStatus.Active,
        Currency = "TRY"
    };
}
