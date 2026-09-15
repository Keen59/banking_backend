using MediatR;

namespace LedgerService.Application.Commands.GetAccountBalance;

public class GetAccountBalanceCommand : IRequest<GetAccountBalanceResponse>
{
    public Guid AccountId { get; init; }

    public Guid RequestedCustomerId { get; init; }

    public bool CanReadAny { get; init; }
}
