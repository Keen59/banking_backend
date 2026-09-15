using LedgerService.Application.DTOs;
using LedgerService.Application.DTOs.Ledger;

namespace LedgerService.Application.Commands.ReleaseHold;

public class ReleaseHoldResponse : Response
{
    public BalanceDto? Balance { get; set; }
}
