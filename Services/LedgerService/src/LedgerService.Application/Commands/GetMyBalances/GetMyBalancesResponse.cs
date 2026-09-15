using LedgerService.Application.DTOs;
using LedgerService.Application.DTOs.Ledger;

namespace LedgerService.Application.Commands.GetMyBalances;

public class GetMyBalancesResponse : Response
{
    public IReadOnlyCollection<BalanceDto> Balances { get; set; } = [];
}
