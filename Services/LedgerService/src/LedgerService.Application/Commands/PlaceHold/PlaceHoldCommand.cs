using MediatR;

namespace LedgerService.Application.Commands.PlaceHold;

public class PlaceHoldCommand : IRequest<PlaceHoldResponse>
{
    public Guid LedgerAccountId { get; init; }

    public string IdempotencyKey { get; init; } = string.Empty;

    public decimal Amount { get; init; }
}
