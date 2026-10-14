using MediatR;
using RiskService.Domain.Enums;

namespace RiskService.Application.Commands.EvaluatePayment;

public class EvaluatePaymentCommand : IRequest
{
    public ERiskChannel Channel { get; set; }

    public Guid SourceId { get; set; }

    public Guid AccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "TRY";

    public ERiskDirection Direction { get; set; }
}
