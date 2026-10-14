using MediatR;
using RiskService.Application.DTOs;
using RiskService.Application.DTOs.RiskAlerts;

namespace RiskService.Application.Commands.CloseAlert;

public class CloseAlertCommand : IRequest<CloseAlertResponse>
{
    public Guid AlertId { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public class CloseAlertResponse : Response
{
    public RiskAlertDto? Data { get; set; }
}
