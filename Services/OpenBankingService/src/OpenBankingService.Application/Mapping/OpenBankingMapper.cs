using OpenBankingService.Application.DTOs.Accounts;
using OpenBankingService.Application.DTOs.Consents;
using OpenBankingService.Application.DTOs.Payments;
using OpenBankingService.Domain.Entities;

namespace OpenBankingService.Application.Mapping;

public static class OpenBankingMapper
{
    public static ConsentDto ToDto(Consent consent)
    {
        return new ConsentDto
        {
            Id = consent.Id,
            CustomerId = consent.CustomerId,
            TppClientId = consent.TppClientId,
            AccessToken = consent.AccessToken,
            Scopes = consent.Scopes,
            Status = consent.Status,
            ExpiresAt = consent.ExpiresAt,
            AccountIds = consent.Accounts.Select(x => x.AccountId).ToList(),
            CreatedAt = consent.CreatedAt
        };
    }

    public static AisAccountDto ToAisDto(AccountProjection account)
    {
        return new AisAccountDto
        {
            AccountId = account.Id,
            Iban = account.Iban,
            Currency = account.Currency
        };
    }

    public static PaymentInitiationDto ToDto(PaymentInitiation initiation)
    {
        return new PaymentInitiationDto
        {
            Id = initiation.Id,
            CustomerId = initiation.CustomerId,
            ConsentId = initiation.ConsentId,
            SourceAccountId = initiation.SourceAccountId,
            DestinationAccountId = initiation.DestinationAccountId,
            Amount = initiation.Amount,
            Currency = initiation.Currency,
            Description = initiation.Description,
            Status = initiation.Status,
            RejectReason = initiation.RejectReason,
            JournalEntryId = initiation.JournalEntryId,
            CreatedAt = initiation.CreatedAt
        };
    }
}
