using LedgerService.Application.DTOs;
using LedgerService.Application.DTOs.Ledger;

namespace LedgerService.Application.Commands.GetAccountBalance;

public class GetAccountBalanceResponse : Response
{
    public BalanceDto? Balance { get; set; }
}
