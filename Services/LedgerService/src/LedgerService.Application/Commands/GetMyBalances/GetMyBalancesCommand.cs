using MediatR;

namespace LedgerService.Application.Commands.GetMyBalances;

public class GetMyBalancesCommand : IRequest<GetMyBalancesResponse>
{
    public Guid CustomerId { get; init; }
}
