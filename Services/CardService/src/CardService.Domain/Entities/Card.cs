using CardService.Domain.Enums;

namespace CardService.Domain.Entities;

public class Card : BaseEntity
{
    public Guid CustomerId { get; set; }

    public Guid AccountId { get; set; }

    public string Token { get; set; } = null!;

    public string Last4 { get; set; } = null!;

    public string Brand { get; set; } = "Virtual";

    public ECardStatus Status { get; set; }
}
