using RiskService.Domain.Entities;

namespace RiskService.Application.Interfaces.Repositories;

public interface IAccountProjectionRepository : IRepository<AccountProjection>
{
    Task<bool> ExistsAsync(Guid accountId, CancellationToken cancellationToken = default);
}
