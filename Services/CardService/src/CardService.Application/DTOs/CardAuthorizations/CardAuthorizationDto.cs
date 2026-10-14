using CardService.Domain.Enums;

namespace CardService.Application.DTOs.CardAuthorizations;

public class CardAuthorizationDto
{
    public Guid Id { get; set; }

    public Guid CardId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid AccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string Merchant { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public EAuthorizationStatus Status { get; set; }

    public string? RejectReason { get; set; }

    public Guid? JournalEntryId { get; set; }
}
