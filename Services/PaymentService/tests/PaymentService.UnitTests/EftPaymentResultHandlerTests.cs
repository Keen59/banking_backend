using Banking.Contracts.Events;
using PaymentService.Application.Commands.CompleteEftPayment;
using PaymentService.Application.Commands.HoldEftPayment;
using PaymentService.Application.Commands.RejectEftPayment;
using PaymentService.Application.Commands.RequestEftReturn;
using PaymentService.Application.Commands.RequestEftSettlement;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Xunit;

namespace PaymentService.UnitTests;

public class EftPaymentResultHandlerTests
{
    [Fact]
    public async Task Hold_sets_held_from_initiated()
    {
        var payment = SeededPayment();
        var store = new InMemoryPaymentUnitOfWork();
        store.EftPaymentRows.Add(payment);

        await new MarkEftPaymentHeldHandler(store).Handle(new MarkEftPaymentHeldCommand
        {
            Event = new EftPaymentHeld(payment.Id, DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Held, payment.Status);
    }

    [Fact]
    public async Task Hold_does_not_overwrite_completed()
    {
        var payment = SeededPayment();
        payment.Status = ETransferStatus.Completed;
        payment.JournalEntryId = Guid.NewGuid();
        var store = new InMemoryPaymentUnitOfWork();
        store.EftPaymentRows.Add(payment);

        await new MarkEftPaymentHeldHandler(store).Handle(new MarkEftPaymentHeldCommand
        {
            Event = new EftPaymentHeld(payment.Id, DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Completed, payment.Status);
    }

    [Fact]
    public async Task Complete_sets_journal_and_completed_status()
    {
        var payment = SeededPayment();
        payment.Status = ETransferStatus.Held;
        var store = new InMemoryPaymentUnitOfWork();
        store.EftPaymentRows.Add(payment);
        var journalId = Guid.NewGuid();

        await new CompleteEftPaymentHandler(store).Handle(new CompleteEftPaymentCommand
        {
            Event = new EftPaymentCompleted(payment.Id, journalId, DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Completed, payment.Status);
        Assert.Equal(journalId, payment.JournalEntryId);
        Assert.Null(payment.RejectReason);
    }

    [Fact]
    public async Task Reject_sets_reason_when_not_completed()
    {
        var payment = SeededPayment();
        payment.Status = ETransferStatus.Held;
        var store = new InMemoryPaymentUnitOfWork();
        store.EftPaymentRows.Add(payment);

        await new RejectEftPaymentHandler(store).Handle(new RejectEftPaymentCommand
        {
            Event = new EftPaymentRejected(payment.Id, "EFT iade.", DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Rejected, payment.Status);
        Assert.Equal("EFT iade.", payment.RejectReason);
    }

    [Fact]
    public async Task Reject_does_not_change_completed_payment()
    {
        var payment = SeededPayment();
        payment.Status = ETransferStatus.Completed;
        payment.JournalEntryId = Guid.NewGuid();
        var store = new InMemoryPaymentUnitOfWork();
        store.EftPaymentRows.Add(payment);

        await new RejectEftPaymentHandler(store).Handle(new RejectEftPaymentCommand
        {
            Event = new EftPaymentRejected(payment.Id, "late reject", DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Completed, payment.Status);
        Assert.Null(payment.RejectReason);
    }

    [Fact]
    public async Task Settlement_from_held_publishes_EftSettlementRequested()
    {
        var payment = SeededPayment();
        payment.Status = ETransferStatus.Held;
        var store = new InMemoryPaymentUnitOfWork();
        store.EftPaymentRows.Add(payment);
        var publisher = new RecordingPublisher();

        var response = await new RequestEftSettlementHandler(store, publisher).Handle(
            new RequestEftSettlementCommand { EftPaymentId = payment.Id },
            CancellationToken.None);

        Assert.Equal(ETransferStatus.Held, response.Data!.Status);
        var published = Assert.Single(publisher.Messages.OfType<EftSettlementRequested>());
        Assert.Equal(payment.Id, published.EftPaymentId);
    }

    [Fact]
    public async Task Settlement_from_initiated_throws()
    {
        var payment = SeededPayment();
        var store = new InMemoryPaymentUnitOfWork();
        store.EftPaymentRows.Add(payment);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RequestEftSettlementHandler(store, new RecordingPublisher()).Handle(
                new RequestEftSettlementCommand { EftPaymentId = payment.Id },
                CancellationToken.None));
    }

    [Fact]
    public async Task Settlement_of_completed_does_not_republish()
    {
        var payment = SeededPayment();
        payment.Status = ETransferStatus.Completed;
        var store = new InMemoryPaymentUnitOfWork();
        store.EftPaymentRows.Add(payment);
        var publisher = new RecordingPublisher();

        var response = await new RequestEftSettlementHandler(store, publisher).Handle(
            new RequestEftSettlementCommand { EftPaymentId = payment.Id },
            CancellationToken.None);

        Assert.Equal("EFT already settled.", response.Message);
        Assert.Empty(publisher.Messages);
    }

    [Fact]
    public async Task Return_from_held_publishes_EftReturnRequested()
    {
        var payment = SeededPayment();
        payment.Status = ETransferStatus.Held;
        var store = new InMemoryPaymentUnitOfWork();
        store.EftPaymentRows.Add(payment);
        var publisher = new RecordingPublisher();

        await new RequestEftReturnHandler(store, publisher).Handle(
            new RequestEftReturnCommand { EftPaymentId = payment.Id, Reason = "iban mismatch" },
            CancellationToken.None);

        var published = Assert.Single(publisher.Messages.OfType<EftReturnRequested>());
        Assert.Equal(payment.Id, published.EftPaymentId);
        Assert.Equal("iban mismatch", published.Reason);
    }

    [Fact]
    public async Task Return_of_settled_throws()
    {
        var payment = SeededPayment();
        payment.Status = ETransferStatus.Completed;
        var store = new InMemoryPaymentUnitOfWork();
        store.EftPaymentRows.Add(payment);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RequestEftReturnHandler(store, new RecordingPublisher()).Handle(
                new RequestEftReturnCommand { EftPaymentId = payment.Id },
                CancellationToken.None));
    }

    private static EftPayment SeededPayment() => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        SourceAccountId = Guid.NewGuid(),
        DestinationIban = "TR330000129999999999999999",
        Amount = 25m,
        Currency = "TRY",
        IdempotencyKey = "k",
        Status = ETransferStatus.Initiated,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
