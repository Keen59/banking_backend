using Banking.Contracts.Events;
using Microsoft.Extensions.Options;
using PaymentService.Application.Commands.CreateTransfer;
using PaymentService.Application.Options;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Xunit;

namespace PaymentService.UnitTests;

public class CreateTransferHandlerTests
{
    [Fact]
    public async Task Handle_initiates_transfer_and_publishes_TransferRequested()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        var store = SeededStore(customerId, sourceId, destinationId);
        var publisher = new RecordingPublisher();
        var handler = new CreateTransferHandler(store, publisher, Limits());

        var response = await handler.Handle(new CreateTransferCommand
        {
            CustomerId = customerId,
            SourceAccountId = sourceId,
            DestinationAccountId = destinationId,
            Amount = 100m,
            Currency = "TRY",
            Description = "İç virman",
            IdempotencyKey = "key-1"
        }, CancellationToken.None);

        Assert.NotNull(response.Data);
        Assert.Equal(ETransferStatus.Initiated, response.Data!.Status);
        Assert.Single(store.TransferRows);
        var published = Assert.Single(publisher.Messages.OfType<TransferRequested>());
        Assert.Equal(response.Data.Id, published.TransferId);
        Assert.Equal(sourceId, published.SourceAccountId);
        Assert.Equal(destinationId, published.DestinationAccountId);
        Assert.Equal(100m, published.Amount);
    }

    [Fact]
    public async Task Handle_same_idempotency_key_returns_existing_without_second_publish()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        var store = SeededStore(customerId, sourceId, destinationId);
        var publisher = new RecordingPublisher();
        var handler = new CreateTransferHandler(store, publisher, Limits());
        var command = new CreateTransferCommand
        {
            CustomerId = customerId,
            SourceAccountId = sourceId,
            DestinationAccountId = destinationId,
            Amount = 50m,
            Currency = "TRY",
            IdempotencyKey = "dup-key"
        };

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.Data!.Id, second.Data!.Id);
        Assert.Single(store.TransferRows);
        Assert.Single(publisher.Messages.OfType<TransferRequested>());
    }

    [Fact]
    public async Task Handle_rejects_amount_over_per_transfer_limit()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        var store = SeededStore(customerId, sourceId, destinationId);
        var handler = new CreateTransferHandler(store, new RecordingPublisher(), Limits());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateTransferCommand
            {
                CustomerId = customerId,
                SourceAccountId = sourceId,
                DestinationAccountId = destinationId,
                Amount = 50_000.01m,
                Currency = "TRY",
                IdempotencyKey = "over-max"
            }, CancellationToken.None));

        Assert.Contains("per-transfer", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(store.TransferRows);
    }

    [Fact]
    public async Task Handle_rejects_when_daily_limit_would_be_exceeded()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        var store = SeededStore(customerId, sourceId, destinationId);
        store.TransferRows.Add(new Transfer
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            SourceAccountId = sourceId,
            DestinationAccountId = destinationId,
            Amount = 90_000m,
            Currency = "TRY",
            IdempotencyKey = "earlier",
            Status = ETransferStatus.Initiated,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var handler = new CreateTransferHandler(store, new RecordingPublisher(), Limits());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateTransferCommand
            {
                CustomerId = customerId,
                SourceAccountId = sourceId,
                DestinationAccountId = destinationId,
                Amount = 20_000m,
                Currency = "TRY",
                IdempotencyKey = "over-daily"
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_rejects_source_that_does_not_belong_to_caller()
    {
        var callerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        var store = SeededStore(Guid.NewGuid(), sourceId, destinationId);
        var handler = new CreateTransferHandler(store, new RecordingPublisher(), Limits());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateTransferCommand
            {
                CustomerId = callerId,
                SourceAccountId = sourceId,
                DestinationAccountId = destinationId,
                Amount = 10m,
                Currency = "TRY",
                IdempotencyKey = "not-owner"
            }, CancellationToken.None));
    }

    private static InMemoryPaymentUnitOfWork SeededStore(
        Guid customerId,
        Guid sourceId,
        Guid destinationId)
    {
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
        return store;
    }

    private static IOptions<TransferLimitOptions> Limits()
        => Options.Create(new TransferLimitOptions
        {
            MaxAmount = 50_000m,
            DailyAmount = 100_000m
        });
}
