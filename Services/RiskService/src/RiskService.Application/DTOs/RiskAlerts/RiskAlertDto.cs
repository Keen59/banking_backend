using RiskService.Domain.Enums;

namespace RiskService.Application.DTOs.RiskAlerts;

public class RiskAlertDto
{
    public Guid Id { get; set; }

    public Guid ScreeningId { get; set; }

    public Guid CustomerId { get; set; }

    public ERiskChannel Channel { get; set; }

    public Guid SourceId { get; set; }

    public string Rule { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public EAlertStatus Status { get; set; }

    public string? CloseReason { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
