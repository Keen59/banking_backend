using MediatR;
using OpenBankingService.Application.DTOs;
using OpenBankingService.Application.DTOs.Consents;

namespace OpenBankingService.Application.Commands.CreateConsent;

public class CreateConsentCommand : IRequest<CreateConsentResponse>
{
    public Guid CustomerId { get; set; }

    public string TppClientId { get; set; } = string.Empty;

    public int Scopes { get; set; }

    public IReadOnlyList<Guid> AccountIds { get; set; } = [];

    public int ExpiresInDays { get; set; } = 90;

    public string IdempotencyKey { get; set; } = string.Empty;
}

public class CreateConsentResponse : Response
{
    public ConsentDto? Data { get; set; }
}
