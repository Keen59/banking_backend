namespace LedgerService.Domain.Entities;

public class JournalEntry : BaseEntity
{
    public string IdempotencyKey { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateTimeOffset BookedAt { get; set; }

    public ICollection<JournalLine> Lines { get; set; } = [];
}
