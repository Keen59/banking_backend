using CardService.Domain.Enums;

namespace CardService.Application.DTOs.Cards;

public class CardDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Guid AccountId { get; set; }

    public string Token { get; set; } = string.Empty;

    public string Last4 { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;

    public ECardStatus Status { get; set; }
}
