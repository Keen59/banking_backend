using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using LoanService.Application.Commands.ApplyLoan;
using LoanService.Application.Options;
using LoanService.Domain.Entities;
using LoanService.Domain.Enums;
using Xunit;

namespace LoanService.UnitTests;

public class ApplyLoanHandlerTests
{
    [Fact]
    public async Task Handle_applies_loan_without_publishing_and_returns_existing_for_same_idempotency_key()
    {
        var customerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var store = SeededStore(accountId, customerId);
        var publisher = new RecordingPublisher();
        var handler = new ApplyLoanHandler(store, Limits());
        var command = new ApplyLoanCommand
        {
            CustomerId = customerId,
            AccountId = accountId,
            Amount = 1_000m,
            TermMonths = 3,
            Currency = "TRY",
            IdempotencyKey = "loan-1"
        };

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(ELoanStatus.Applied, first.Data!.Status);
        Assert.Equal(first.Data.Id, second.Data!.Id);
        Assert.Single(store.LoanRows);
        Assert.Empty(publisher.Messages);
    }

    [Fact]
    public async Task Handle_rejects_amount_over_limit()
    {
        var customerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var store = SeededStore(accountId, customerId);
        var handler = new ApplyLoanHandler(store, Limits());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new ApplyLoanCommand
            {
                CustomerId = customerId,
                AccountId = accountId,
                Amount = 100_000.01m,
                TermMonths = 3,
                Currency = "TRY",
                IdempotencyKey = "over-limit"
            }, CancellationToken.None));

        Assert.Contains("limit", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(store.LoanRows);
    }

    [Fact]
    public async Task Handle_rejects_term_months_outside_range()
    {
        var customerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var store = SeededStore(accountId, customerId);
        var handler = new ApplyLoanHandler(store, Limits());
        var command = new ApplyLoanCommand
        {
            CustomerId = customerId,
            AccountId = accountId,
            Amount = 1_000m,
            TermMonths = 2,
            Currency = "TRY",
            IdempotencyKey = "short-term"
        };

        var validation = await new ApplyLoanCommandValidator().TestValidateAsync(command);
        validation.ShouldHaveValidationErrorFor(x => x.TermMonths);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.Contains("TermMonths", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(store.LoanRows);
    }

    [Fact]
    public async Task Handle_rejects_other_customers_account()
    {
        var accountId = Guid.NewGuid();
        var store = SeededStore(accountId, Guid.NewGuid());
        var handler = new ApplyLoanHandler(store, Limits());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new ApplyLoanCommand
            {
                CustomerId = Guid.NewGuid(),
                AccountId = accountId,
                Amount = 1_000m,
                TermMonths = 6,
                Currency = "TRY",
                IdempotencyKey = "foreign-account"
            }, CancellationToken.None));

        Assert.Empty(store.LoanRows);
    }

    private static InMemoryLoanUnitOfWork SeededStore(Guid accountId, Guid ownerId)
    {
        var store = new InMemoryLoanUnitOfWork();
        store.Accounts.Add(new AccountProjection
        {
            Id = accountId,
            CustomerId = ownerId,
            Currency = "TRY",
            Status = EAccountProjectionStatus.Active
        });
        return store;
    }

    private static IOptions<LoanLimitOptions> Limits()
        => Options.Create(new LoanLimitOptions { MaxAmount = 100_000m });
}
