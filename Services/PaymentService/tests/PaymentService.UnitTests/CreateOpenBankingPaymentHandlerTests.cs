using Banking.Contracts.Events;
using Microsoft.Extensions.Options;
using PaymentService.Application.Commands.CreateOpenBankingPayment;
using PaymentService.Application.Options;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Xunit;

namespace PaymentService.UnitTests;

public class CreateOpenBankingPaymentHandlerTests
{
    [Fact]
    public async Task Handle_creates_transfer_with_initiation_id()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        var initiationId = Guid.NewGuid();
        var store = new InMemoryPaymentUnitOfWork();
        store.Accounts.Add(new AccountProjection
        {
            Id = sourceId,
            CustomerId = customerId,
            Iban = "TR330010012345678901234567",
            Currency = "TRY",
            Status = EAccountProjectionStatus.Active
        });
        store.Accounts.Add(new AccountProjection
        {
            Id = destinationId,
            CustomerId = Guid.NewGuid(),
            Iban = "TR330010098765432109876543",
            Currency = "TRY",
            Status = EAccountProjectionStatus.Active
        });

        var publisher = new RecordingPublisher();
        var handler = new CreateOpenBankingPaymentHandler(
            store,
            publisher,
            Options.Create(new TransferLimitOptions { MaxAmount = 50_000m, DailyAmount = 100_000m }));

        await handler.Handle(new CreateOpenBankingPaymentCommand
        {
            Event = new OpenBankingPaymentRequested(
                initiationId,
                customerId,
                sourceId,
                destinationId,
                50m,
                "TRY",
                "PIS",
                DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        var transfer = Assert.Single(store.TransferRows);
        Assert.Equal(initiationId, transfer.Id);
        Assert.Equal($"open-banking:{initiationId:D}", transfer.IdempotencyKey);
        Assert.Single(publisher.Messages.OfType<TransferRequested>());
    }
}
