namespace RiskService.Domain.Entities;

public class AccountProjection : BaseEntity
{
    public Guid CustomerId { get; set; }

    public string Currency { get; set; } = "TRY";
}
