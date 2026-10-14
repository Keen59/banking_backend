using OpenBankingService.Domain.Entities;

namespace OpenBankingService.Application.Interfaces.Repositories;

public interface IAccountProjectionRepository : IRepository<AccountProjection>
{
    Task<bool> ExistsAsync(Guid accountId, CancellationToken cancellationToken = default);
}
