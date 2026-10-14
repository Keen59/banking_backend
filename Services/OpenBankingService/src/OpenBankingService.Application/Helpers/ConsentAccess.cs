using OpenBankingService.Domain.Entities;
using OpenBankingService.Domain.Enums;

namespace OpenBankingService.Application.Helpers;

public static class ConsentAccess
{
    public static bool HasScope(int storedScopes, EConsentScope scope)
        => (storedScopes & (int)scope) == (int)scope;

    public static bool HasAisOrPis(int storedScopes)
        => HasScope(storedScopes, EConsentScope.Ais) || HasScope(storedScopes, EConsentScope.Pis);

    public static Consent RequireActive(
        Consent? consent,
        EConsentScope requiredScope,
        DateTimeOffset now)
    {
        if (consent is null)
            throw new UnauthorizedAccessException("Consent token is invalid.");

        if (consent.Status != EConsentStatus.Active)
            throw new InvalidOperationException("Consent is not active.");

        if (consent.ExpiresAt <= now)
            throw new InvalidOperationException("Consent has expired.");

        if (!HasScope(consent.Scopes, requiredScope))
            throw new InvalidOperationException("Consent does not include the required scope.");

        return consent;
    }
}
