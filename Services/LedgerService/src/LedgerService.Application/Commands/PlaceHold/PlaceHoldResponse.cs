using LedgerService.Application.DTOs;
using LedgerService.Application.DTOs.Ledger;

namespace LedgerService.Application.Commands.PlaceHold;

public class PlaceHoldResponse : Response
{
    public Guid HoldId { get; set; }

    public BalanceDto? Balance { get; set; }
}
