using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;

namespace LedgerService.Application.Helpers;

public static class LedgerBalanceCalculator
{
    public static (decimal Ledger, decimal Hold, decimal Available) Calculate(
        ELedgerAccountKind kind,
        IEnumerable<JournalLine> lines,
        IEnumerable<AccountHold> activeHolds)
    {
        var debit = lines.Where(line => line.Side == EEntrySide.Debit).Sum(line => line.Amount);
        var credit = lines.Where(line => line.Side == EEntrySide.Credit).Sum(line => line.Amount);
        var ledger = kind == ELedgerAccountKind.CustomerDemandDeposit
            ? credit - debit
            : debit - credit;
        var hold = activeHolds.Sum(item => item.Amount);
        return (ledger, hold, ledger - hold);
    }
}
