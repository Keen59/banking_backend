using MediatR;
using RiskService.Application.DTOs;
using RiskService.Application.DTOs.RiskAlerts;

namespace RiskService.Application.Commands.GetOpenAlerts;

public class GetOpenAlertsCommand : IRequest<GetOpenAlertsResponse>;

public class GetOpenAlertsResponse : Response
{
    public IReadOnlyList<RiskAlertDto> Data { get; set; } = [];
}
