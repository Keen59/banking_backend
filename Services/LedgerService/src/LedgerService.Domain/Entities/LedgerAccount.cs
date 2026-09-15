using LedgerService.Domain.Enums;

namespace LedgerService.Domain.Entities;

public class LedgerAccount : BaseEntity
{
    public Guid? SourceAccountId { get; set; }

    public Guid? CustomerId { get; set; }

    public string Currency { get; set; } = "TRY";

    public ELedgerAccountKind Kind { get; set; }

    public ELedgerAccountStatus Status { get; set; }

    public ICollection<JournalLine> Lines { get; set; } = [];

    public ICollection<AccountHold> Holds { get; set; } = [];
}
