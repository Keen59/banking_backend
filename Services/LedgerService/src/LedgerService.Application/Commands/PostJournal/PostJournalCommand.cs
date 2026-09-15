using LedgerService.Application.DTOs.Ledger;
using MediatR;

namespace LedgerService.Application.Commands.PostJournal;

public class PostJournalCommand : IRequest<PostJournalResponse>
{
    public string IdempotencyKey { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<JournalLineInput> Lines { get; init; } = [];
}
