using LedgerService.Domain.Enums;

namespace LedgerService.Application.DTOs.Ledger;

public class JournalLineInput
{
    public Guid LedgerAccountId { get; set; }

    public EEntrySide Side { get; set; }

    public decimal Amount { get; set; }
}
