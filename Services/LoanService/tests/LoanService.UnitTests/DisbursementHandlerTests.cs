using Banking.Contracts.Events;
using LoanService.Application.Commands.CompleteDisbursement;
using LoanService.Domain.Entities;
using LoanService.Domain.Enums;
using Xunit;

namespace LoanService.UnitTests;

public class DisbursementHandlerTests
{
    [Fact]
    public async Task Posted_creates_equal_installments_with_remainder_on_last()
    {
        var loan = AppliedLoan(1_000m, 3);
        var store = StoreWith(loan);
        var publisher = new RecordingPublisher();

        await new CompleteDisbursementHandler(store, publisher).Handle(
            new CompleteDisbursementCommand
            {
                Event = new LoanDisbursementPosted(loan.Id, Guid.NewGuid(), DateTimeOffset.UtcNow)
            },
            CancellationToken.None);

        Assert.Equal(ELoanStatus.Disbursed, loan.Status);
        Assert.Equal(1_000m, loan.RemainingPrincipal);
        Assert.Equal(3, store.InstallmentRows.Count);
        Assert.Equal(333.33m, store.InstallmentRows.Single(x => x.Sequence == 1).Amount);
        Assert.Equal(333.33m, store.InstallmentRows.Single(x => x.Sequence == 2).Amount);
        Assert.Equal(333.34m, store.InstallmentRows.Single(x => x.Sequence == 3).Amount);
        Assert.Equal(1_000m, store.InstallmentRows.Sum(x => x.Amount));
        Assert.All(store.InstallmentRows, x => Assert.Equal(EInstallmentStatus.Pending, x.Status));
        var notification = Assert.Single(publisher.Messages.OfType<NotificationRequested>());
        Assert.Equal("loan.disbursed", notification.Type);
    }

    [Fact]
    public async Task Same_posted_twice_does_not_duplicate_installments()
    {
        var loan = AppliedLoan(1_000m, 3);
        var store = StoreWith(loan);
        var handler = new CompleteDisbursementHandler(store, new RecordingPublisher());
        var command = new CompleteDisbursementCommand
        {
            Event = new LoanDisbursementPosted(loan.Id, Guid.NewGuid(), DateTimeOffset.UtcNow)
        };

        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(3, store.InstallmentRows.Count);
        Assert.Equal(ELoanStatus.Disbursed, loan.Status);
    }

    private static Loan AppliedLoan(decimal amount, int termMonths)
    {
        return new Loan
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = amount,
            Currency = "TRY",
            TermMonths = termMonths,
            RemainingPrincipal = 0m,
            IdempotencyKey = "k",
            Status = ELoanStatus.Applied
        };
    }

    private static InMemoryLoanUnitOfWork StoreWith(Loan loan)
    {
        var store = new InMemoryLoanUnitOfWork();
        store.LoanRows.Add(loan);
        return store;
    }
}
