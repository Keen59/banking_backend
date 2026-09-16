using Banking.Contracts.Events;
using Microsoft.Extensions.Options;
using PaymentService.Application.Commands.CreateFastPayment;
using PaymentService.Application.Helpers;
using PaymentService.Application.Options;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Xunit;

namespace PaymentService.UnitTests;

public class CreateFastPaymentHandlerTests
{
    [Fact]
    public async Task Handle_initiates_fast_and_publishes_FastPaymentRequested()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var destinationIban = TurkishIban.Build("00012", "9999999999999999");
        var store = SeededStore(customerId, sourceId);
        var publisher = new RecordingPublisher();
        var handler = new CreateFastPaymentHandler(store, publisher, Limits());

        var response = await handler.Handle(new CreateFastPaymentCommand
        {
            CustomerId = customerId,
            SourceAccountId = sourceId,
            DestinationIban = destinationIban.ToLowerInvariant(),
            Amount = 80m,
            Currency = "TRY",
            Description = "FAST",
            IdempotencyKey = "fast-1"
        }, CancellationToken.None);

        Assert.NotNull(response.Data);
        Assert.Equal(ETransferStatus.Initiated, response.Data!.Status);
        Assert.Equal(destinationIban, response.Data.DestinationIban);
        var published = Assert.Single(publisher.Messages.OfType<FastPaymentRequested>());
        Assert.Equal(response.Data.Id, published.FastPaymentId);
        Assert.Equal(destinationIban, published.DestinationIban);
    }

    [Fact]
    public async Task Handle_same_idempotency_key_returns_existing_without_second_publish()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var store = SeededStore(customerId, sourceId);
        var publisher = new RecordingPublisher();
        var handler = new CreateFastPaymentHandler(store, publisher, Limits());
        var command = new CreateFastPaymentCommand
        {
            CustomerId = customerId,
            SourceAccountId = sourceId,
            DestinationIban = TurkishIban.Build("00012", "9999999999999999"),
            Amount = 40m,
            Currency = "TRY",
            IdempotencyKey = "dup-fast"
        };

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.Data!.Id, second.Data!.Id);
        Assert.Single(store.FastPaymentRows);
        Assert.Single(publisher.Messages.OfType<FastPaymentRequested>());
    }

    [Fact]
    public async Task Handle_rejects_internal_destination_iban()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var store = SeededStore(customerId, sourceId);
        var internalIban = store.Accounts[1].Iban;
        var handler = new CreateFastPaymentHandler(store, new RecordingPublisher(), Limits());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateFastPaymentCommand
            {
                CustomerId = customerId,
                SourceAccountId = sourceId,
                DestinationIban = internalIban,
                Amount = 10m,
                Currency = "TRY",
                IdempotencyKey = "internal-iban"
            }, CancellationToken.None));

        Assert.Contains("/api/payments/transfers", exception.Message, StringComparison.Ordinal);
        Assert.Empty(store.FastPaymentRows);
    }

    [Fact]
    public async Task Handle_rejects_invalid_iban()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var store = SeededStore(customerId, sourceId);
        var handler = new CreateFastPaymentHandler(store, new RecordingPublisher(), Limits());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateFastPaymentCommand
            {
                CustomerId = customerId,
                SourceAccountId = sourceId,
                DestinationIban = "TR000000000000000000000000",
                Amount = 10m,
                Currency = "TRY",
                IdempotencyKey = "bad-iban"
            }, CancellationToken.None));
    }

    private static InMemoryPaymentUnitOfWork SeededStore(Guid customerId, Guid sourceId)
    {
        var store = new InMemoryPaymentUnitOfWork();
        store.Accounts.Add(new AccountProjection
        {
            Id = sourceId,
            CustomerId = customerId,
            Iban = TurkishIban.Build("00100", "1234567890123456"),
            Currency = "TRY",
            Status = EAccountProjectionStatus.Active
        });
        store.Accounts.Add(new AccountProjection
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Iban = TurkishIban.Build("00100", "9876543210987654"),
            Currency = "TRY",
            Status = EAccountProjectionStatus.Active
        });
        return store;
    }

    private static IOptions<TransferLimitOptions> Limits()
        => Options.Create(new TransferLimitOptions
        {
            MaxAmount = 50_000m,
            DailyAmount = 100_000m,
            MaxTestCredit = 100_000m
        });
}
