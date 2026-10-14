using CardService.Application.DTOs.Cards;
using CardService.Domain.Entities;

namespace CardService.Application.Mapping;

public static class CardMapper
{
    public static CardDto ToDto(Card card)
    {
        return new CardDto
        {
            Id = card.Id,
            CustomerId = card.CustomerId,
            AccountId = card.AccountId,
            Token = card.Token,
            Last4 = card.Last4,
            Brand = card.Brand,
            Status = card.Status
        };
    }
}
