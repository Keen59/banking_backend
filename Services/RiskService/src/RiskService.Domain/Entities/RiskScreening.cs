using RiskService.Domain.Enums;

namespace RiskService.Domain.Entities;

public class RiskScreening : BaseEntity
{
    public Guid CustomerId { get; set; }

    public ERiskChannel Channel { get; set; }

    public Guid SourceId { get; set; }

    public Guid AccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public ERiskDirection Direction { get; set; }
}
