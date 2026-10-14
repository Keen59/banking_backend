using Banking.Contracts.Events;
using LoanService.Application.Commands.ApproveLoan;
using LoanService.Application.Commands.RejectLoan;
using LoanService.Domain.Entities;
using LoanService.Domain.Enums;
using Xunit;

namespace LoanService.UnitTests;

public class ApproveRejectLoanHandlerTests
{
    [Fact]
    public async Task Approve_from_applied_publishes_disbursement_requested()
    {
        var loan = AppliedLoan();
        var store = StoreWith(loan);
        var publisher = new RecordingPublisher();

        var response = await new ApproveLoanHandler(store, publisher).Handle(
            new ApproveLoanCommand { LoanId = loan.Id },
            CancellationToken.None);

        Assert.Equal(ELoanStatus.Applied, response.Data!.Status);
        var published = Assert.Single(publisher.Messages.OfType<LoanDisbursementRequested>());
        Assert.Equal(loan.Id, published.LoanId);
        Assert.Equal(loan.AccountId, published.AccountId);
        Assert.Equal(loan.Amount, published.Amount);
    }

    [Fact]
    public async Task Approve_from_disbursed_throws()
    {
        var loan = AppliedLoan();
        loan.Status = ELoanStatus.Disbursed;
        var store = StoreWith(loan);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ApproveLoanHandler(store, new RecordingPublisher()).Handle(
                new ApproveLoanCommand { LoanId = loan.Id },
                CancellationToken.None));
    }

    [Fact]
    public async Task Reject_from_applied_sets_rejected()
    {
        var loan = AppliedLoan();
        var store = StoreWith(loan);
        var publisher = new RecordingPublisher();

        var response = await new RejectLoanHandler(store, publisher).Handle(
            new RejectLoanCommand { LoanId = loan.Id, Reason = "risk" },
            CancellationToken.None);

        Assert.Equal(ELoanStatus.Rejected, loan.Status);
        Assert.Equal("risk", loan.RejectReason);
        Assert.Equal(ELoanStatus.Rejected, response.Data!.Status);
        var notification = Assert.Single(publisher.Messages.OfType<NotificationRequested>());
        Assert.Equal("loan.rejected", notification.Type);
    }

    [Fact]
    public async Task Reject_from_disbursed_throws()
    {
        var loan = AppliedLoan();
        loan.Status = ELoanStatus.Disbursed;
        var store = StoreWith(loan);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RejectLoanHandler(store, new RecordingPublisher()).Handle(
                new RejectLoanCommand { LoanId = loan.Id, Reason = "too late" },
                CancellationToken.None));
    }

    private static Loan AppliedLoan()
    {
        return new Loan
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Amount = 1_000m,
            Currency = "TRY",
            TermMonths = 3,
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
