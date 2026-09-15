using LedgerService.Application.DTOs;
using LedgerService.Application.DTOs.Ledger;

namespace LedgerService.Application.Commands.GetAccountMovements;

public class GetAccountMovementsResponse : Response
{
    public IReadOnlyCollection<MovementDto> Movements { get; set; } = [];
}
