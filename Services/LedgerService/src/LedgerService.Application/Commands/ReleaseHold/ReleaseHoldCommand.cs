using MediatR;

namespace LedgerService.Application.Commands.ReleaseHold;

public class ReleaseHoldCommand : IRequest<ReleaseHoldResponse>
{
    public string IdempotencyKey { get; init; } = string.Empty;
}
