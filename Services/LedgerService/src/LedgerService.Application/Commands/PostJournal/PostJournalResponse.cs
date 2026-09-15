using LedgerService.Application.DTOs;

namespace LedgerService.Application.Commands.PostJournal;

public class PostJournalResponse : Response
{
    public Guid JournalEntryId { get; set; }
}
