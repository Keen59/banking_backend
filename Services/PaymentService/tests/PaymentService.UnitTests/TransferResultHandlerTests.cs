using Banking.Contracts.Events;
using PaymentService.Application.Commands.CompleteTransfer;
using PaymentService.Application.Commands.RejectTransfer;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Xunit;

namespace PaymentService.UnitTests;

public class TransferResultHandlerTests
{
    [Fact]
    public async Task Complete_sets_journal_and_completed_status()
    {
        var transfer = SeededTransfer();
        var store = new InMemoryPaymentUnitOfWork();
        store.TransferRows.Add(transfer);
        var journalId = Guid.NewGuid();

        await new CompleteTransferHandler(store).Handle(new CompleteTransferCommand
        {
            Event = new TransferCompleted(transfer.Id, journalId, DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Completed, transfer.Status);
        Assert.Equal(journalId, transfer.JournalEntryId);
        Assert.Null(transfer.RejectReason);
    }

    [Fact]
    public async Task Complete_does_not_overwrite_already_completed()
    {
        var transfer = SeededTransfer();
        transfer.Status = ETransferStatus.Completed;
        transfer.JournalEntryId = Guid.NewGuid();
        var store = new InMemoryPaymentUnitOfWork();
        store.TransferRows.Add(transfer);
        var originalJournal = transfer.JournalEntryId;

        await new CompleteTransferHandler(store).Handle(new CompleteTransferCommand
        {
            Event = new TransferCompleted(transfer.Id, Guid.NewGuid(), DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Completed, transfer.Status);
        Assert.Equal(originalJournal, transfer.JournalEntryId);
    }

    [Fact]
    public async Task Reject_sets_reason_when_not_completed()
    {
        var transfer = SeededTransfer();
        var store = new InMemoryPaymentUnitOfWork();
        store.TransferRows.Add(transfer);

        await new RejectTransferHandler(store).Handle(new RejectTransferCommand
        {
            Event = new TransferRejected(transfer.Id, "Available yetersiz.", DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Rejected, transfer.Status);
        Assert.Equal("Available yetersiz.", transfer.RejectReason);
    }

    [Fact]
    public async Task Reject_does_not_change_completed_transfer()
    {
        var transfer = SeededTransfer();
        transfer.Status = ETransferStatus.Completed;
        transfer.JournalEntryId = Guid.NewGuid();
        var store = new InMemoryPaymentUnitOfWork();
        store.TransferRows.Add(transfer);

        await new RejectTransferHandler(store).Handle(new RejectTransferCommand
        {
            Event = new TransferRejected(transfer.Id, "late reject", DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Completed, transfer.Status);
        Assert.Null(transfer.RejectReason);
    }

    private static Transfer SeededTransfer() => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        SourceAccountId = Guid.NewGuid(),
        DestinationAccountId = Guid.NewGuid(),
        Amount = 25m,
        Currency = "TRY",
        IdempotencyKey = "k",
        Status = ETransferStatus.Initiated,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
