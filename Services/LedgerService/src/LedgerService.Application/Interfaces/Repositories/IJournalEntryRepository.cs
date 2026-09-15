using LedgerService.Domain.Entities;

namespace LedgerService.Application.Interfaces.Repositories;

public interface IJournalEntryRepository : IRepository<JournalEntry>
{
    Task<JournalEntry?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JournalLine>> GetLinesByLedgerAccountIdAsync(
        Guid ledgerAccountId,
        CancellationToken cancellationToken = default);
}
