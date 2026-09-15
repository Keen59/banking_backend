using LedgerService.Application.DTOs;

namespace LedgerService.Application.Commands.ExecuteInternalTransfer;

public class ExecuteInternalTransferResponse : Response
{
    public Guid JournalEntryId { get; set; }
}
