using MediatR;
using RiskService.Application.DTOs;
using RiskService.Application.DTOs.RiskAlerts;

namespace RiskService.Application.Commands.GetAlert;

public class GetAlertCommand : IRequest<GetAlertResponse>
{
    public Guid AlertId { get; set; }
}

public class GetAlertResponse : Response
{
    public RiskAlertDto? Data { get; set; }
}
