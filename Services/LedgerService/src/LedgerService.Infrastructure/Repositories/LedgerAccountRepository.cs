using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Domain.Entities;
using LedgerService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace LedgerService.Infrastructure.Repositories;

public class LedgerAccountRepository(DBContext context) : Repository<LedgerAccount>(context), ILedgerAccountRepository
{
    public async Task<LedgerAccount?> GetBySourceAccountIdAsync(
        Guid sourceAccountId,
        CancellationToken cancellationToken = default)
    {
        return await Context.LedgerAccount
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SourceAccountId == sourceAccountId, cancellationToken);
    }

    public async Task<IReadOnlyList<LedgerAccount>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await Context.LedgerAccount
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsBySourceAccountIdAsync(
        Guid sourceAccountId,
        CancellationToken cancellationToken = default)
    {
        return await Context.LedgerAccount.AnyAsync(x => x.SourceAccountId == sourceAccountId, cancellationToken);
    }

    public async Task<IReadOnlyList<LedgerAccount>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        return await Context.LedgerAccount
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }
}
