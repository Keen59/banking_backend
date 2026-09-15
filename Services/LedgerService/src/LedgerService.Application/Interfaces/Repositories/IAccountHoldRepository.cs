using LedgerService.Domain.Entities;

namespace LedgerService.Application.Interfaces.Repositories;

public interface IAccountHoldRepository : IRepository<AccountHold>
{
    Task<AccountHold?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountHold>> GetActiveByLedgerAccountIdAsync(
        Guid ledgerAccountId,
        CancellationToken cancellationToken = default);
}
