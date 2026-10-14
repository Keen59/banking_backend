using MediatR;
using RiskService.Application.Interfaces.Repositories;
using RiskService.Application.Mapping;
using RiskService.Domain.Enums;

namespace RiskService.Application.Commands.CloseAlert;

public class CloseAlertHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CloseAlertCommand, CloseAlertResponse>
{
    public async Task<CloseAlertResponse> Handle(
        CloseAlertCommand request,
        CancellationToken cancellationToken)
    {
        var alert = await unitOfWork.RiskAlerts.GetByIdAsync(request.AlertId)
            ?? throw new KeyNotFoundException("Risk alert not found.");

        if (alert.Status != EAlertStatus.Open)
            throw new InvalidOperationException("Risk alert is not open.");

        alert.Status = EAlertStatus.Closed;
        alert.CloseReason = request.Reason;
        alert.ClosedAt = DateTimeOffset.UtcNow;
        unitOfWork.RiskAlerts.Update(alert);
        await unitOfWork.SaveAsync(cancellationToken);

        return new CloseAlertResponse
        {
            Message = "Risk alert closed.",
            Data = RiskAlertMapper.ToDto(alert)
        };
    }
}
