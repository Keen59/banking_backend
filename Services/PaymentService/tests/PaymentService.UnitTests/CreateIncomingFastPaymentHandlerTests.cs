using Banking.Contracts.Events;
using Microsoft.Extensions.Options;
using PaymentService.Application.Commands.CompleteIncomingFastPayment;
using PaymentService.Application.Commands.CreateIncomingFastPayment;
using PaymentService.Application.Helpers;
using PaymentService.Application.Options;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Xunit;

namespace PaymentService.UnitTests;

public class CreateIncomingFastPaymentHandlerTests
{
    [Fact]
    public async Task Handle_initiates_incoming_fast_and_publishes_IncomingFastPaymentRequested()
    {
        var ownerId = Guid.NewGuid();
        var destinationIban = TurkishIban.Build("00100", "1234567890123456");
        var sourceIban = TurkishIban.Build("00012", "9999999999999999");
        var store = SeededStore(ownerId, destinationIban);
        var publisher = new RecordingPublisher();
        var handler = new CreateIncomingFastPaymentHandler(store, publisher, Limits());

        var response = await handler.Handle(new CreateIncomingFastPaymentCommand
        {
            RequestedByCustomerId = Guid.NewGuid(),
            DestinationIban = destinationIban.ToLowerInvariant(),
            SourceIban = sourceIban,
            Amount = 80m,
            Currency = "TRY",
            Description = "Gelen FAST",
            IdempotencyKey = "in-fast-1"
        }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Initiated, response.Data!.Status);
        Assert.Equal(destinationIban, response.Data.DestinationIban);
        Assert.Equal(sourceIban, response.Data.SourceIban);
        Assert.Equal(ownerId, response.Data.AccountCustomerId);
        var published = Assert.Single(publisher.Messages.OfType<IncomingFastPaymentRequested>());
        Assert.Equal(response.Data.Id, published.IncomingFastPaymentId);
        Assert.Equal(store.Accounts[0].Id, published.DestinationAccountId);
    }

    [Fact]
    public async Task Handle_same_idempotency_key_returns_existing_without_second_publish()
    {
        var destinationIban = TurkishIban.Build("00100", "1234567890123456");
        var store = SeededStore(Guid.NewGuid(), destinationIban);
        var publisher = new RecordingPublisher();
        var handler = new CreateIncomingFastPaymentHandler(store, publisher, Limits());
        var command = new CreateIncomingFastPaymentCommand
        {
            RequestedByCustomerId = Guid.NewGuid(),
            DestinationIban = destinationIban,
            Amount = 40m,
            Currency = "TRY",
            IdempotencyKey = "dup-in-fast"
        };

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.Data!.Id, second.Data!.Id);
        Assert.Single(store.IncomingFastPaymentRows);
        Assert.Single(publisher.Messages.OfType<IncomingFastPaymentRequested>());
    }

    [Fact]
    public async Task Handle_rejects_unknown_destination_iban()
    {
        var store = SeededStore(Guid.NewGuid(), TurkishIban.Build("00100", "1234567890123456"));
        var handler = new CreateIncomingFastPaymentHandler(store, new RecordingPublisher(), Limits());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateIncomingFastPaymentCommand
            {
                RequestedByCustomerId = Guid.NewGuid(),
                DestinationIban = TurkishIban.Build("00012", "9999999999999999"),
                Amount = 10m,
                Currency = "TRY",
                IdempotencyKey = "unknown-iban"
            }, CancellationToken.None));

        Assert.Contains("internal account", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(store.IncomingFastPaymentRows);
    }

    [Fact]
    public async Task Handle_rejects_internal_source_iban()
    {
        var destinationIban = TurkishIban.Build("00100", "1234567890123456");
        var store = SeededStore(Guid.NewGuid(), destinationIban);
        var internalSource = TurkishIban.Build("00100", "9876543210987654");
        store.Accounts.Add(new AccountProjection
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Iban = internalSource,
            Currency = "TRY",
            Status = EAccountProjectionStatus.Active
        });
        var handler = new CreateIncomingFastPaymentHandler(store, new RecordingPublisher(), Limits());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateIncomingFastPaymentCommand
            {
                RequestedByCustomerId = Guid.NewGuid(),
                DestinationIban = destinationIban,
                SourceIban = internalSource,
                Amount = 10m,
                Currency = "TRY",
                IdempotencyKey = "internal-source"
            }, CancellationToken.None));

        Assert.Contains("/api/payments/transfers", exception.Message, StringComparison.Ordinal);
        Assert.Empty(store.IncomingFastPaymentRows);
    }

    [Fact]
    public async Task Handle_does_not_count_toward_outbound_daily_limit()
    {
        var destinationIban = TurkishIban.Build("00100", "1234567890123456");
        var store = SeededStore(Guid.NewGuid(), destinationIban);
        store.FastPaymentRows.Add(new FastPayment
        {
            Id = Guid.NewGuid(),
            CustomerId = store.Accounts[0].CustomerId,
            SourceAccountId = store.Accounts[0].Id,
            DestinationIban = TurkishIban.Build("00012", "1111111111111111"),
            Amount = 90_000m,
            Currency = "TRY",
            IdempotencyKey = "out-today",
            Status = ETransferStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow
        });
        var publisher = new RecordingPublisher();
        var handler = new CreateIncomingFastPaymentHandler(store, publisher, Limits());

        var response = await handler.Handle(new CreateIncomingFastPaymentCommand
        {
            RequestedByCustomerId = Guid.NewGuid(),
            DestinationIban = destinationIban,
            Amount = 40_000m,
            Currency = "TRY",
            IdempotencyKey = "in-after-out"
        }, CancellationToken.None);

        Assert.NotNull(response.Data);
        Assert.Single(publisher.Messages.OfType<IncomingFastPaymentRequested>());
    }

    [Fact]
    public async Task Complete_sets_journal_and_completed_status()
    {
        var payment = new IncomingFastPayment
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            AccountCustomerId = Guid.NewGuid(),
            RequestedByCustomerId = Guid.NewGuid(),
            DestinationIban = TurkishIban.Build("00100", "1234567890123456"),
            Amount = 100m,
            Currency = "TRY",
            IdempotencyKey = "k",
            Status = ETransferStatus.Initiated
        };
        var store = new InMemoryPaymentUnitOfWork();
        store.IncomingFastPaymentRows.Add(payment);
        var journalId = Guid.NewGuid();

        await new CompleteIncomingFastPaymentHandler(store).Handle(new CompleteIncomingFastPaymentCommand
        {
            Event = new IncomingFastPaymentCompleted(payment.Id, journalId, DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Completed, payment.Status);
        Assert.Equal(journalId, payment.JournalEntryId);
    }

    private static InMemoryPaymentUnitOfWork SeededStore(Guid ownerId, string destinationIban)
    {
        var store = new InMemoryPaymentUnitOfWork();
        store.Accounts.Add(new AccountProjection
        {
            Id = Guid.NewGuid(),
            CustomerId = ownerId,
            Iban = destinationIban,
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
