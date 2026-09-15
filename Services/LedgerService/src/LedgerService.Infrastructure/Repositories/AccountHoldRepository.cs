using LedgerService.Application.Interfaces.Repositories;
using LedgerService.Domain.Entities;
using LedgerService.Domain.Enums;
using LedgerService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace LedgerService.Infrastructure.Repositories;

public class AccountHoldRepository(DBContext context) : Repository<AccountHold>(context), IAccountHoldRepository
{
    public async Task<AccountHold?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await Context.AccountHold
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<IReadOnlyList<AccountHold>> GetActiveByLedgerAccountIdAsync(
        Guid ledgerAccountId,
        CancellationToken cancellationToken = default)
    {
        return await Context.AccountHold
            .AsNoTracking()
            .Where(x => x.LedgerAccountId == ledgerAccountId && x.Status == EHoldStatus.Active)
            .ToListAsync(cancellationToken);
    }
}
