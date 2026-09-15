using LedgerService.Domain.Enums;

namespace LedgerService.Domain.Entities;

public class JournalLine : BaseEntity
{
    public Guid JournalEntryId { get; set; }

    public Guid LedgerAccountId { get; set; }

    public EEntrySide Side { get; set; }

    public decimal Amount { get; set; }

    public JournalEntry JournalEntry { get; set; } = null!;

    public LedgerAccount LedgerAccount { get; set; } = null!;
}
