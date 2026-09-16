using Banking.Contracts.Events;
using Microsoft.Extensions.Options;
using PaymentService.Application.Commands.CreateEftPayment;
using PaymentService.Application.Helpers;
using PaymentService.Application.Options;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Xunit;

namespace PaymentService.UnitTests;

public class CreateEftPaymentHandlerTests
{
    [Fact]
    public async Task Handle_initiates_eft_and_publishes_EftPaymentRequested()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var destinationIban = TurkishIban.Build("00012", "9999999999999999");
        var store = SeededStore(customerId, sourceId);
        var publisher = new RecordingPublisher();
        var handler = new CreateEftPaymentHandler(store, publisher, Limits());

        var response = await handler.Handle(new CreateEftPaymentCommand
        {
            CustomerId = customerId,
            SourceAccountId = sourceId,
            DestinationIban = destinationIban.ToLowerInvariant(),
            Amount = 80m,
            Currency = "TRY",
            Description = "EFT",
            IdempotencyKey = "eft-1"
        }, CancellationToken.None);

        Assert.NotNull(response.Data);
        Assert.Equal(ETransferStatus.Initiated, response.Data!.Status);
        Assert.Equal(destinationIban, response.Data.DestinationIban);
        var published = Assert.Single(publisher.Messages.OfType<EftPaymentRequested>());
        Assert.Equal(response.Data.Id, published.EftPaymentId);
        Assert.Equal(destinationIban, published.DestinationIban);
    }

    [Fact]
    public async Task Handle_same_idempotency_key_returns_existing_without_second_publish()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var store = SeededStore(customerId, sourceId);
        var publisher = new RecordingPublisher();
        var handler = new CreateEftPaymentHandler(store, publisher, Limits());
        var command = new CreateEftPaymentCommand
        {
            CustomerId = customerId,
            SourceAccountId = sourceId,
            DestinationIban = TurkishIban.Build("00012", "9999999999999999"),
            Amount = 40m,
            Currency = "TRY",
            IdempotencyKey = "dup-eft"
        };

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.Data!.Id, second.Data!.Id);
        Assert.Single(store.EftPaymentRows);
        Assert.Single(publisher.Messages.OfType<EftPaymentRequested>());
    }

    [Fact]
    public async Task Handle_rejects_internal_destination_iban()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var store = SeededStore(customerId, sourceId);
        var internalIban = store.Accounts[1].Iban;
        var handler = new CreateEftPaymentHandler(store, new RecordingPublisher(), Limits());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateEftPaymentCommand
            {
                CustomerId = customerId,
                SourceAccountId = sourceId,
                DestinationIban = internalIban,
                Amount = 10m,
                Currency = "TRY",
                IdempotencyKey = "internal-iban"
            }, CancellationToken.None));

        Assert.Contains("/api/payments/transfers", exception.Message, StringComparison.Ordinal);
        Assert.Empty(store.EftPaymentRows);
    }

    [Fact]
    public async Task Handle_rejects_invalid_iban()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var store = SeededStore(customerId, sourceId);
        var handler = new CreateEftPaymentHandler(store, new RecordingPublisher(), Limits());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateEftPaymentCommand
            {
                CustomerId = customerId,
                SourceAccountId = sourceId,
                DestinationIban = "TR000000000000000000000000",
                Amount = 10m,
                Currency = "TRY",
                IdempotencyKey = "bad-iban"
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_rejects_when_shared_daily_limit_would_be_exceeded()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var store = SeededStore(customerId, sourceId);
        store.FastPaymentRows.Add(new FastPayment
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            SourceAccountId = sourceId,
            DestinationIban = TurkishIban.Build("00012", "1111111111111111"),
            Amount = 90_000m,
            Currency = "TRY",
            IdempotencyKey = "fast-today",
            Status = ETransferStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow
        });
        var handler = new CreateEftPaymentHandler(store, new RecordingPublisher(), Limits());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateEftPaymentCommand
            {
                CustomerId = customerId,
                SourceAccountId = sourceId,
                DestinationIban = TurkishIban.Build("00012", "9999999999999999"),
                Amount = 20_000m,
                Currency = "TRY",
                IdempotencyKey = "over-daily"
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_counts_held_eft_toward_daily_limit()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var store = SeededStore(customerId, sourceId);
        store.EftPaymentRows.Add(new EftPayment
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            SourceAccountId = sourceId,
            DestinationIban = TurkishIban.Build("00012", "1111111111111111"),
            Amount = 90_000m,
            Currency = "TRY",
            IdempotencyKey = "held-today",
            Status = ETransferStatus.Held,
            CreatedAt = DateTimeOffset.UtcNow
        });
        var handler = new CreateEftPaymentHandler(store, new RecordingPublisher(), Limits());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateEftPaymentCommand
            {
                CustomerId = customerId,
                SourceAccountId = sourceId,
                DestinationIban = TurkishIban.Build("00012", "9999999999999999"),
                Amount = 20_000m,
                Currency = "TRY",
                IdempotencyKey = "after-held"
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_excludes_rejected_eft_from_daily_limit()
    {
        var customerId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var store = SeededStore(customerId, sourceId);
        store.EftPaymentRows.Add(new EftPayment
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            SourceAccountId = sourceId,
            DestinationIban = TurkishIban.Build("00012", "1111111111111111"),
            Amount = 90_000m,
            Currency = "TRY",
            IdempotencyKey = "rejected-today",
            Status = ETransferStatus.Rejected,
            CreatedAt = DateTimeOffset.UtcNow
        });
        var publisher = new RecordingPublisher();
        var handler = new CreateEftPaymentHandler(store, publisher, Limits());

        var response = await handler.Handle(new CreateEftPaymentCommand
        {
            CustomerId = customerId,
            SourceAccountId = sourceId,
            DestinationIban = TurkishIban.Build("00012", "9999999999999999"),
            Amount = 20_000m,
            Currency = "TRY",
            IdempotencyKey = "after-rejected"
        }, CancellationToken.None);

        Assert.NotNull(response.Data);
        Assert.Single(publisher.Messages.OfType<EftPaymentRequested>());
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
