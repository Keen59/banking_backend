using LedgerService.Application.Commands.GetAccountStatement;
using LedgerService.Application.Commands.PostJournal;
using LedgerService.Application.DTOs.Ledger;
using LedgerService.Domain;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using Xunit;

namespace LedgerService.UnitTests;

public class GetAccountStatementHandlerTests
{
    [Fact]
    public async Task Statement_splits_opening_period_and_closing()
    {
        var store = SeededStore(out var source);
        var openingAt = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var periodAt = new DateTimeOffset(2026, 2, 10, 0, 0, 0, TimeSpan.Zero);
        await Credit(store, source.Id, 100m, "open", openingAt);
        await Credit(store, source.Id, 40m, "period", periodAt);

        var response = await new GetAccountStatementHandler(store).Handle(
            new GetAccountStatementCommand
            {
                AccountId = source.SourceAccountId!.Value,
                RequestedCustomerId = source.CustomerId!.Value,
                From = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
                To = new DateTimeOffset(2026, 2, 28, 0, 0, 0, TimeSpan.Zero)
            },
            CancellationToken.None);

        Assert.Equal(100m, response.Statement!.OpeningLedger);
        Assert.Equal(140m, response.Statement.ClosingLedger);
        var movement = Assert.Single(response.Statement.Movements);
        Assert.Equal(40m, movement.Amount);
        Assert.Equal(EEntrySide.Credit, movement.Side);
    }

    [Fact]
    public async Task Statement_rejects_inverted_range()
    {
        var store = SeededStore(out var source);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new GetAccountStatementHandler(store).Handle(
                new GetAccountStatementCommand
                {
                    AccountId = source.SourceAccountId!.Value,
                    RequestedCustomerId = source.CustomerId!.Value,
                    From = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
                    To = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero)
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task Statement_rejects_other_customer_without_permission()
    {
        var store = SeededStore(out var source);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            new GetAccountStatementHandler(store).Handle(
                new GetAccountStatementCommand
                {
                    AccountId = source.SourceAccountId!.Value,
                    RequestedCustomerId = Guid.NewGuid(),
                    CanReadAny = false,
                    From = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    To = new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero)
                },
                CancellationToken.None));
    }

    private static InMemoryLedgerUnitOfWork SeededStore(out LedgerAccount source)
    {
        var store = new InMemoryLedgerUnitOfWork();
        store.Accounts.Add(new LedgerAccount
        {
            Id = SystemLedgerAccounts.InternalClearingTry,
            Currency = "TRY",
            Kind = ELedgerAccountKind.InternalClearing,
            Status = ELedgerAccountStatus.Active
        });

        source = new LedgerAccount
        {
            Id = Guid.NewGuid(),
            SourceAccountId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Currency = "TRY",
            Kind = ELedgerAccountKind.CustomerDemandDeposit,
            Status = ELedgerAccountStatus.Active
        };
        store.Accounts.Add(source);
        return store;
    }

    private static async Task Credit(
        InMemoryLedgerUnitOfWork store,
        Guid customerLedgerAccountId,
        decimal amount,
        string key,
        DateTimeOffset bookedAt)
    {
        await new PostJournalHandler(store).Handle(new PostJournalCommand
        {
            IdempotencyKey = key,
            Description = key,
            Lines =
            [
                new JournalLineInput
                {
                    LedgerAccountId = SystemLedgerAccounts.InternalClearingTry,
                    Side = EEntrySide.Debit,
                    Amount = amount
                },
                new JournalLineInput
                {
                    LedgerAccountId = customerLedgerAccountId,
                    Side = EEntrySide.Credit,
                    Amount = amount
                }
            ]
        }, CancellationToken.None);

        store.Entries.Single(entry => entry.IdempotencyKey == key).BookedAt = bookedAt;
    }
}
