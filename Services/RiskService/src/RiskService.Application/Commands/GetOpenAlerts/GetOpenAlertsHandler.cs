using MediatR;
using RiskService.Application.Interfaces.Repositories;
using RiskService.Application.Mapping;

namespace RiskService.Application.Commands.GetOpenAlerts;

public class GetOpenAlertsHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetOpenAlertsCommand, GetOpenAlertsResponse>
{
    public async Task<GetOpenAlertsResponse> Handle(
        GetOpenAlertsCommand request,
        CancellationToken cancellationToken)
    {
        var alerts = await unitOfWork.RiskAlerts.GetOpenAsync(cancellationToken);

        return new GetOpenAlertsResponse
        {
            Message = "Open risk alerts listed.",
            Data = alerts.Select(RiskAlertMapper.ToDto).ToList()
        };
    }
}
