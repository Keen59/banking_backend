using OpenBankingService.Domain.Enums;

namespace OpenBankingService.Application.DTOs.Consents;

public class ConsentDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string TppClientId { get; set; } = string.Empty;

    public string AccessToken { get; set; } = string.Empty;

    public int Scopes { get; set; }

    public EConsentStatus Status { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public IReadOnlyList<Guid> AccountIds { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
}
