using LedgerService.Domain.Enums;

namespace LedgerService.Application.DTOs.Ledger;

public class MovementDto
{
    public Guid JournalEntryId { get; set; }

    public DateTimeOffset BookedAt { get; set; }

    public string Description { get; set; } = string.Empty;

    public EEntrySide Side { get; set; }

    public decimal Amount { get; set; }
}
