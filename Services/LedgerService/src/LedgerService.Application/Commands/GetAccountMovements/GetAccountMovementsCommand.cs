using MediatR;

namespace LedgerService.Application.Commands.GetAccountMovements;

public class GetAccountMovementsCommand : IRequest<GetAccountMovementsResponse>
{
    public Guid AccountId { get; init; }

    public Guid RequestedCustomerId { get; init; }

    public bool CanReadAny { get; init; }
}
