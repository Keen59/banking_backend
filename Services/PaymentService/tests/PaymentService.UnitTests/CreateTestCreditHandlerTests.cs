using Banking.Contracts.Events;
using Microsoft.Extensions.Options;
using PaymentService.Application.Commands.CompleteTestCredit;
using PaymentService.Application.Commands.CreateTestCredit;
using PaymentService.Application.Options;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Xunit;

namespace PaymentService.UnitTests;

public class CreateTestCreditHandlerTests
{
    [Fact]
    public async Task Handle_initiates_test_credit_and_publishes_TestCreditRequested()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var store = SeededStore(accountId, ownerId);
        var publisher = new RecordingPublisher();
        var handler = new CreateTestCreditHandler(store, publisher, Limits());

        var response = await handler.Handle(new CreateTestCreditCommand
        {
            RequestedByCustomerId = Guid.NewGuid(),
            AccountId = accountId,
            Amount = 1_000m,
            Currency = "TRY",
            IdempotencyKey = "credit-1"
        }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Initiated, response.Data!.Status);
        Assert.Equal(ownerId, response.Data.AccountCustomerId);
        var published = Assert.Single(publisher.Messages.OfType<TestCreditRequested>());
        Assert.Equal(accountId, published.AccountId);
        Assert.Equal(1_000m, published.Amount);
    }

    [Fact]
    public async Task Handle_same_idempotency_key_returns_existing_without_second_publish()
    {
        var accountId = Guid.NewGuid();
        var store = SeededStore(accountId, Guid.NewGuid());
        var publisher = new RecordingPublisher();
        var handler = new CreateTestCreditHandler(store, publisher, Limits());
        var command = new CreateTestCreditCommand
        {
            RequestedByCustomerId = Guid.NewGuid(),
            AccountId = accountId,
            Amount = 250m,
            Currency = "TRY",
            IdempotencyKey = "dup-credit"
        };

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(first.Data!.Id, second.Data!.Id);
        Assert.Single(store.TestCreditRows);
        Assert.Single(publisher.Messages.OfType<TestCreditRequested>());
    }

    [Fact]
    public async Task Handle_rejects_amount_over_test_credit_limit()
    {
        var accountId = Guid.NewGuid();
        var store = SeededStore(accountId, Guid.NewGuid());
        var handler = new CreateTestCreditHandler(store, new RecordingPublisher(), Limits());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateTestCreditCommand
            {
                RequestedByCustomerId = Guid.NewGuid(),
                AccountId = accountId,
                Amount = 100_000.01m,
                Currency = "TRY",
                IdempotencyKey = "over-credit"
            }, CancellationToken.None));

        Assert.Contains("test-credit", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(store.TestCreditRows);
    }

    [Fact]
    public async Task Complete_sets_journal_and_completed_status()
    {
        var credit = new TestCredit
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            AccountCustomerId = Guid.NewGuid(),
            RequestedByCustomerId = Guid.NewGuid(),
            Amount = 100m,
            Currency = "TRY",
            IdempotencyKey = "k",
            Status = ETransferStatus.Initiated
        };
        var store = new InMemoryPaymentUnitOfWork();
        store.TestCreditRows.Add(credit);
        var journalId = Guid.NewGuid();

        await new CompleteTestCreditHandler(store).Handle(new CompleteTestCreditCommand
        {
            Event = new TestCreditPosted(credit.Id, journalId, DateTimeOffset.UtcNow)
        }, CancellationToken.None);

        Assert.Equal(ETransferStatus.Completed, credit.Status);
        Assert.Equal(journalId, credit.JournalEntryId);
    }

    private static InMemoryPaymentUnitOfWork SeededStore(Guid accountId, Guid ownerId)
    {
        var store = new InMemoryPaymentUnitOfWork();
        store.Accounts.Add(new AccountProjection
        {
            Id = accountId,
            CustomerId = ownerId,
            Iban = "TR330010012345678901234567",
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
