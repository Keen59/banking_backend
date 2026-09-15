using LedgerService.Application.Helpers;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using Xunit;

namespace LedgerService.UnitTests;

public class LedgerBalanceCalculatorTests
{
    [Fact]
    public void Customer_demand_deposit_starts_at_zero_without_postings()
    {
        var (ledger, hold, available) = LedgerBalanceCalculator.Calculate(
            ELedgerAccountKind.CustomerDemandDeposit,
            [],
            []);

        Assert.Equal(0m, ledger);
        Assert.Equal(0m, hold);
        Assert.Equal(0m, available);
    }

    [Fact]
    public void Customer_credit_increases_ledger_and_available()
    {
        var lines = new[]
        {
            new JournalLine { Side = EEntrySide.Credit, Amount = 80m }
        };
        var (ledger, hold, available) = LedgerBalanceCalculator.Calculate(
            ELedgerAccountKind.CustomerDemandDeposit,
            lines,
            []);

        Assert.Equal(80m, ledger);
        Assert.Equal(0m, hold);
        Assert.Equal(80m, available);
    }
}
