using LedgerService.Application.DTOs;
using LedgerService.Application.DTOs.Ledger;

namespace LedgerService.Application.Commands.GetAccountStatement;

public class GetAccountStatementResponse : Response
{
    public StatementDto? Statement { get; set; }
}
