using MediatR;
using RiskService.Application.Interfaces.Repositories;
using RiskService.Application.Mapping;

namespace RiskService.Application.Commands.GetAlert;

public class GetAlertHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAlertCommand, GetAlertResponse>
{
    public async Task<GetAlertResponse> Handle(
        GetAlertCommand request,
        CancellationToken cancellationToken)
    {
        var alert = await unitOfWork.RiskAlerts.GetByIdAsync(request.AlertId)
            ?? throw new KeyNotFoundException("Risk alert not found.");

        return new GetAlertResponse
        {
            Message = "Risk alert retrieved.",
            Data = RiskAlertMapper.ToDto(alert)
        };
    }
}
