using RiskService.Application.DTOs.RiskAlerts;
using RiskService.Domain.Entities;

namespace RiskService.Application.Mapping;

public static class RiskAlertMapper
{
    public static RiskAlertDto ToDto(RiskAlert alert)
    {
        return new RiskAlertDto
        {
            Id = alert.Id,
            ScreeningId = alert.ScreeningId,
            CustomerId = alert.CustomerId,
            Channel = alert.Channel,
            SourceId = alert.SourceId,
            Rule = alert.Rule,
            Amount = alert.Amount,
            Status = alert.Status,
            CloseReason = alert.CloseReason,
            ClosedAt = alert.ClosedAt,
            CreatedAt = alert.CreatedAt
        };
    }
}
