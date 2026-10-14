using Banking.Contracts.Events;
using OpenBankingService.Application.Commands.CompletePaymentInitiation;
using OpenBankingService.Domain.Entities;
using OpenBankingService.Domain.Enums;
using Xunit;

namespace OpenBankingService.UnitTests;

public class CompletePaymentInitiationHandlerTests
{
    [Fact]
    public async Task Handle_sets_completed_and_does_not_overwrite()
    {
        var initiation = new PaymentInitiation
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            ConsentId = Guid.NewGuid(),
            SourceAccountId = Guid.NewGuid(),
            DestinationAccountId = Guid.NewGuid(),
            Amount = 10m,
            Currency = "TRY",
            Description = "Virman",
            IdempotencyKey = "k",
            Status = EInitiationStatus.Initiated
        };
        var store = new InMemoryOpenBankingUnitOfWork();
        store.InitiationRows.Add(initiation);
        var handler = new CompletePaymentInitiationHandler(store);

        var firstJournal = Guid.NewGuid();
        await handler.Handle(
            new CompletePaymentInitiationCommand
            {
                Event = new TransferCompleted(initiation.Id, firstJournal, DateTimeOffset.UtcNow)
            },
            CancellationToken.None);

        Assert.Equal(EInitiationStatus.Completed, initiation.Status);
        Assert.Equal(firstJournal, initiation.JournalEntryId);

        await handler.Handle(
            new CompletePaymentInitiationCommand
            {
                Event = new TransferCompleted(initiation.Id, Guid.NewGuid(), DateTimeOffset.UtcNow)
            },
            CancellationToken.None);

        Assert.Equal(EInitiationStatus.Completed, initiation.Status);
        Assert.Equal(firstJournal, initiation.JournalEntryId);
    }
}
