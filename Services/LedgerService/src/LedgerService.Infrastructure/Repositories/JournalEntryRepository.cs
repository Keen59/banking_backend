using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Domain.Entities;
using LedgerService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace LedgerService.Infrastructure.Repositories;

public class JournalEntryRepository(DBContext context) : Repository<JournalEntry>(context), IJournalEntryRepository
{
    public async Task<JournalEntry?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await Context.JournalEntry
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<IReadOnlyList<JournalLine>> GetLinesByLedgerAccountIdAsync(
        Guid ledgerAccountId,
        CancellationToken cancellationToken = default)
    {
        return await Context.JournalLine
            .AsNoTracking()
            .Include(x => x.JournalEntry)
            .Where(x => x.LedgerAccountId == ledgerAccountId)
            .ToListAsync(cancellationToken);
    }
}
