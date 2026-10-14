using Banking.Contracts.Events;
using LoanService.Application.Commands.CompleteRepayment;
using LoanService.Application.Commands.RequestRepayment;
using LoanService.Domain.Entities;
using LoanService.Domain.Enums;
using Xunit;

namespace LoanService.UnitTests;

public class RepaymentHandlerTests
{
    [Fact]
    public async Task Repay_from_disbursed_publishes_first_pending_installment()
    {
        var customerId = Guid.NewGuid();
        var store = DisbursedStore(customerId, out var loan);
        var publisher = new RecordingPublisher();

        await new RequestRepaymentHandler(store, publisher).Handle(
            new RequestRepaymentCommand { CustomerId = customerId, LoanId = loan.Id },
            CancellationToken.None);

        var published = Assert.Single(publisher.Messages.OfType<LoanRepaymentRequested>());
        var first = store.InstallmentRows.Single(x => x.Sequence == 1);
        Assert.Equal(loan.Id, published.LoanId);
        Assert.Equal(first.Id, published.InstallmentId);
        Assert.Equal(first.Amount, published.Amount);
        Assert.Equal(EInstallmentStatus.Pending, first.Status);
    }

    [Fact]
    public async Task Repay_from_applied_throws()
    {
        var customerId = Guid.NewGuid();
        var loan = new Loan
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            AccountId = Guid.NewGuid(),
            Amount = 1_000m,
            Currency = "TRY",
            TermMonths = 3,
            IdempotencyKey = "k",
            Status = ELoanStatus.Applied
        };
        var store = new InMemoryLoanUnitOfWork();
        store.LoanRows.Add(loan);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RequestRepaymentHandler(store, new RecordingPublisher()).Handle(
                new RequestRepaymentCommand { CustomerId = customerId, LoanId = loan.Id },
                CancellationToken.None));
    }

    [Fact]
    public async Task Repayment_posted_marks_paid_reduces_principal_and_completes_on_last()
    {
        var customerId = Guid.NewGuid();
        var store = DisbursedStore(customerId, out var loan);
        var handler = new CompleteRepaymentHandler(store, new RecordingPublisher());

        foreach (var installment in store.InstallmentRows.OrderBy(x => x.Sequence).ToList())
        {
            await handler.Handle(
                new CompleteRepaymentCommand
                {
                    Event = new LoanRepaymentPosted(loan.Id, installment.Id, Guid.NewGuid(), DateTimeOffset.UtcNow)
                },
                CancellationToken.None);
        }

        Assert.All(store.InstallmentRows, x => Assert.Equal(EInstallmentStatus.Paid, x.Status));
        Assert.Equal(0m, loan.RemainingPrincipal);
        Assert.Equal(ELoanStatus.Completed, loan.Status);
    }

    private static InMemoryLoanUnitOfWork DisbursedStore(Guid customerId, out Loan loan)
    {
        loan = new Loan
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            AccountId = Guid.NewGuid(),
            Amount = 1_000m,
            Currency = "TRY",
            TermMonths = 3,
            RemainingPrincipal = 1_000m,
            IdempotencyKey = "k",
            Status = ELoanStatus.Disbursed
        };

        var store = new InMemoryLoanUnitOfWork();
        store.LoanRows.Add(loan);
        store.InstallmentRows.AddRange(
        [
            Installment(loan.Id, 1, 333.33m),
            Installment(loan.Id, 2, 333.33m),
            Installment(loan.Id, 3, 333.34m)
        ]);
        return store;
    }

    private static LoanInstallment Installment(Guid loanId, int sequence, decimal amount)
    {
        return new LoanInstallment
        {
            Id = Guid.NewGuid(),
            LoanId = loanId,
            Sequence = sequence,
            DueDate = DateTimeOffset.UtcNow.AddMonths(sequence),
            Amount = amount,
            Status = EInstallmentStatus.Pending
        };
    }
}
