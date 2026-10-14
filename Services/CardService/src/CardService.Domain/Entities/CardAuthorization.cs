using CardService.Domain.Enums;

namespace CardService.Domain.Entities;

public class CardAuthorization : BaseEntity
{
    public Guid CardId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid AccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public string Merchant { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = null!;

    public EAuthorizationStatus Status { get; set; }

    public string? RejectReason { get; set; }

    public Guid? JournalEntryId { get; set; }
}
