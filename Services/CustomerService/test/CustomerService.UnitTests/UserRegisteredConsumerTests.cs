using Banking.Contracts.Events;
using CustomerService.Application.Interfaces.Repositories;
using CustomerService.Application.Interfaces.Services;
using CustomerService.Domain.Entities;
using CustomerService.Domain.Enums;
using CustomerService.Infrastructure.Consumers;
using MassTransit;
using NSubstitute;
using Xunit;

namespace CustomerService.UnitTests;

public class UserRegisteredConsumerTests
{
    [Fact]
    public async Task Consume_creates_cif_stub_when_customer_is_missing()
    {
        var customerId = Guid.NewGuid();
        var customers = Substitute.For<ICustomerRepository>();
        customers.GetByIdAsync(customerId).Returns((Customer?)null);

        var auditLogs = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.CustomerRepository.Returns(customers);
        unitOfWork.AuditLogRepository.Returns(auditLogs);

        var cif = Substitute.For<ICifNumberService>();
        cif.GenerateAsync(Arg.Any<CancellationToken>()).Returns("000000000001");

        var context = Substitute.For<ConsumeContext<UserRegistered>>();
        context.Message.Returns(new UserRegistered(
            Guid.NewGuid(),
            customerId,
            "user@example.com",
            "user",
            "5550000000",
            DateTimeOffset.UtcNow));
        context.CancellationToken.Returns(CancellationToken.None);

        var consumer = new UserRegisteredConsumer(unitOfWork, cif);
        await consumer.Consume(context);

        await customers.Received(1).AddAsync(
            Arg.Is<Customer>(customer =>
                customer.Id == customerId &&
                customer.CifNumber == "000000000001" &&
                customer.Status == ECustomerStatus.Prospect &&
                customer.KycStatus == EKycStatus.NotStarted &&
                customer.Email == "user@example.com"),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_does_not_insert_when_customer_id_already_exists()
    {
        var customerId = Guid.NewGuid();
        var customers = Substitute.For<ICustomerRepository>();
        customers.GetByIdAsync(customerId).Returns(new Customer
        {
            Id = customerId,
            CifNumber = "000000000001",
            Email = "existing@example.com",
            PhoneNumber = "5550000000",
            FirstName = "Existing"
        });

        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.CustomerRepository.Returns(customers);

        var cif = Substitute.For<ICifNumberService>();
        var context = Substitute.For<ConsumeContext<UserRegistered>>();
        context.Message.Returns(new UserRegistered(
            Guid.NewGuid(),
            customerId,
            "user@example.com",
            "user",
            "5550000000",
            DateTimeOffset.UtcNow));
        context.CancellationToken.Returns(CancellationToken.None);

        var consumer = new UserRegisteredConsumer(unitOfWork, cif);
        await consumer.Consume(context);

        await cif.DidNotReceive().GenerateAsync(Arg.Any<CancellationToken>());
        await customers.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }
}
