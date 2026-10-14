using Banking.Contracts.Events;
using Microsoft.Extensions.Options;
using PaymentService.Application.Commands.CompleteIncomingEftPayment;
using PaymentService.Application.Commands.CreateIncomingEftPayment;
using PaymentService.Application.Commands.HoldIncomingEftPayment;
using PaymentService.Application.Commands.RequestIncomingEftReturn;
using PaymentService.Application.Commands.RequestIncomingEftSettlement;
using PaymentService.Application.Helpers;
using PaymentService.Application.Options;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Xunit;

namespace PaymentService.UnitTests;

public class CreateIncomingEftPaymentHandlerTests
{
    [Fact]
    public async Task Handle_initiates_incoming_eft_and_publishes_IncomingEftPaymentRequested()
    {
        var ownerId = Guid.NewGuid();
        var destinationIban = TurkishIban.Build("00100", "1234567890123456");
        var sourceIban = TurkishIban.Build("00012", "9999999999999999");
        var store = SeededStore(ownerId, destinationIban);
        var publisher = new RecordingPublisher();
        var handler = new CreateIncomingEftPaymentHandler(store, publisher, Limits());

        var response = await handler.Handle(new CreateIncomingEftPaymentCommand
        {
            RequestedByCustomerId = Guid.NewGuid(),
            DestinationIban = destinationIban.ToLowerInvariant(),
            SourceIban = sourceIban,
            Amount = 80m,
            Currency = "TRY",
            Description = "Gelen EFT",
            IdempotencyKey = "in-eft-1"
        }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Initiated, response.Data!.Status);
        Assert.Equal(destinationIban, response.Data.DestinationIban);
        var published = Assert.Single(publisher.Messages.OfType<IncomingEftPaymentRequested>());
        Assert.Equal(response.Data.Id, published.IncomingEftPaymentId);
        Assert.Equal(store.Accounts[0].Id, published.DestinationAccountId);
    }

    [Fact]
    public async Task Handle_same_idempotency_key_returns_existing_without_second_publish()
    {
        var destinationIban = TurkishIban.Build("00100", "1234567890123456");
        var store = SeededStore(Guid.NewGuid(), destinationIban);
        var publisher = new RecordingPublisher();
        var handler = new CreateIncomingEftPaymentHandler(store, publisher, Limits());
        var command = new CreateIncomingEftPaymentCommand
        {
            RequestedByCustomerId = Guid.NewGuid(),
            DestinationIban = destinationIban,
            Amount = 40m,
            Currency = "TRY",
            IdempotencyKey = "dup-in-eft"
        };

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.Data!.Id, second.Data!.Id);
        Assert.Single(store.IncomingEftPaymentRows);
        Assert.Single(publisher.Messages.OfType<IncomingEftPaymentRequested>());
    }

    [Fact]
    public async Task Handle_rejects_unknown_destination_iban()
    {
        var store = SeededStore(Guid.NewGuid(), TurkishIban.Build("00100", "1234567890123456"));
        var handler = new CreateIncomingEftPaymentHandler(store, new RecordingPublisher(), Limits());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateIncomingEftPaymentCommand
            {
                RequestedByCustomerId = Guid.NewGuid(),
                DestinationIban = TurkishIban.Build("00012", "9999999999999999"),
                Amount = 10m,
                Currency = "TRY",
                IdempotencyKey = "unknown-iban"
            }, CancellationToken.None));

        Assert.Contains("internal account", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(store.IncomingEftPaymentRows);
    }

    [Fact]
    public async Task Handle_does_not_count_toward_outbound_daily_limit()
    {
        var destinationIban = TurkishIban.Build("00100", "1234567890123456");
        var store = SeededStore(Guid.NewGuid(), destinationIban);
        store.EftPaymentRows.Add(new EftPayment
        {
            Id = Guid.NewGuid(),
            CustomerId = store.Accounts[0].CustomerId,
            SourceAccountId = store.Accounts[0].Id,
            DestinationIban = TurkishIban.Build("00012", "1111111111111111"),
            Amount = 90_000m,
            Currency = "TRY",
            IdempotencyKey = "out-today",
            Status = ETransferStatus.Held,
            CreatedAt = DateTimeOffset.UtcNow
        });
        var publisher = new RecordingPublisher();
        var handler = new CreateIncomingEftPaymentHandler(store, publisher, Limits());

        var response = await handler.Handle(new CreateIncomingEftPaymentCommand
        {
            RequestedByCustomerId = Guid.NewGuid(),
            DestinationIban = destinationIban,
            Amount = 40_000m,
            Currency = "TRY",
            IdempotencyKey = "in-after-out"
        }, CancellationToken.None);

        Assert.NotNull(response.Data);
        Assert.Single(publisher.Messages.OfType<IncomingEftPaymentRequested>());
    }

    [Fact]
    public async Task Hold_sets_held_from_initiated()
    {
        var payment = SeededPayment();
        var store = new InMemoryPaymentUnitOfWork();
        store.IncomingEftPaymentRows.Add(payment);

        await new MarkIncomingEftPaymentHeldHandler(store, new RecordingPublisher()).Handle(new MarkIncomingEftPaymentHeldCommand
        {
            Event = new IncomingEftPaymentHeld(payment.Id, DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Held, payment.Status);
    }

    [Fact]
    public async Task Settlement_from_held_publishes_IncomingEftSettlementRequested()
    {
        var payment = SeededPayment();
        payment.Status = ETransferStatus.Held;
        var store = new InMemoryPaymentUnitOfWork();
        store.IncomingEftPaymentRows.Add(payment);
        var publisher = new RecordingPublisher();

        await new RequestIncomingEftSettlementHandler(store, publisher).Handle(
            new RequestIncomingEftSettlementCommand { IncomingEftPaymentId = payment.Id },
            CancellationToken.None);

        var published = Assert.Single(publisher.Messages.OfType<IncomingEftSettlementRequested>());
        Assert.Equal(payment.Id, published.IncomingEftPaymentId);
        Assert.Equal(payment.AccountId, published.DestinationAccountId);
    }

    [Fact]
    public async Task Settlement_from_initiated_throws()
    {
        var payment = SeededPayment();
        var store = new InMemoryPaymentUnitOfWork();
        store.IncomingEftPaymentRows.Add(payment);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RequestIncomingEftSettlementHandler(store, new RecordingPublisher()).Handle(
                new RequestIncomingEftSettlementCommand { IncomingEftPaymentId = payment.Id },
                CancellationToken.None));
    }

    [Fact]
    public async Task Return_from_held_publishes_IncomingEftReturnRequested()
    {
        var payment = SeededPayment();
        payment.Status = ETransferStatus.Held;
        var store = new InMemoryPaymentUnitOfWork();
        store.IncomingEftPaymentRows.Add(payment);
        var publisher = new RecordingPublisher();

        await new RequestIncomingEftReturnHandler(store, publisher).Handle(
            new RequestIncomingEftReturnCommand { IncomingEftPaymentId = payment.Id, Reason = "iban mismatch" },
            CancellationToken.None);

        var published = Assert.Single(publisher.Messages.OfType<IncomingEftReturnRequested>());
        Assert.Equal("iban mismatch", published.Reason);
    }

    [Fact]
    public async Task Complete_sets_journal_and_completed_status()
    {
        var payment = SeededPayment();
        payment.Status = ETransferStatus.Held;
        var store = new InMemoryPaymentUnitOfWork();
        store.IncomingEftPaymentRows.Add(payment);
        var journalId = Guid.NewGuid();

        await new CompleteIncomingEftPaymentHandler(store, new RecordingPublisher()).Handle(new CompleteIncomingEftPaymentCommand
        {
            Event = new IncomingEftPaymentCompleted(payment.Id, journalId, DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Completed, payment.Status);
        Assert.Equal(journalId, payment.JournalEntryId);
    }

    private static IncomingEftPayment SeededPayment() => new()
    {
        Id = Guid.NewGuid(),
        AccountId = Guid.NewGuid(),
        AccountCustomerId = Guid.NewGuid(),
        RequestedByCustomerId = Guid.NewGuid(),
        DestinationIban = TurkishIban.Build("00100", "1234567890123456"),
        Amount = 25m,
        Currency = "TRY",
        IdempotencyKey = "k",
        Status = ETransferStatus.Initiated,
        CreatedAt = DateTimeOffset.UtcNow
    };

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
