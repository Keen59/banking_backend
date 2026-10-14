using OpenBankingService.Domain.Enums;

namespace OpenBankingService.Domain.Entities;

public class Consent : BaseEntity
{
    public Guid CustomerId { get; set; }

    public string TppClientId { get; set; } = null!;

    public string AccessToken { get; set; } = null!;

    public int Scopes { get; set; }

    public EConsentStatus Status { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public string IdempotencyKey { get; set; } = null!;

    public ICollection<ConsentAccount> Accounts { get; set; } = new List<ConsentAccount>();
}
