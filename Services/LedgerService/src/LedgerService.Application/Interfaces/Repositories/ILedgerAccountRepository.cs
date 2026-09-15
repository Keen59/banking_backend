using LedgerService.Domain.Entities;

namespace LedgerService.Application.Interfaces.Repositories;

public interface ILedgerAccountRepository : IRepository<LedgerAccount>
{
    Task<LedgerAccount?> GetBySourceAccountIdAsync(Guid sourceAccountId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LedgerAccount>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<bool> ExistsBySourceAccountIdAsync(Guid sourceAccountId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LedgerAccount>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
}
