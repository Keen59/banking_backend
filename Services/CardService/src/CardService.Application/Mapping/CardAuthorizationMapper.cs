using CardService.Application.DTOs.CardAuthorizations;
using CardService.Domain.Entities;

namespace CardService.Application.Mapping;

public static class CardAuthorizationMapper
{
    public static CardAuthorizationDto ToDto(CardAuthorization authorization)
    {
        return new CardAuthorizationDto
        {
            Id = authorization.Id,
            CardId = authorization.CardId,
            CustomerId = authorization.CustomerId,
            AccountId = authorization.AccountId,
            Amount = authorization.Amount,
            Currency = authorization.Currency,
            Merchant = authorization.Merchant,
            Description = authorization.Description,
            Status = authorization.Status,
            RejectReason = authorization.RejectReason,
            JournalEntryId = authorization.JournalEntryId
        };
    }
}
