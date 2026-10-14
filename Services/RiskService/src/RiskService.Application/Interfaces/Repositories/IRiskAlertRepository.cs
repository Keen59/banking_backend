using RiskService.Domain.Entities;

namespace RiskService.Application.Interfaces.Repositories;

public interface IRiskAlertRepository : IRepository<RiskAlert>
{
    Task<IReadOnlyList<RiskAlert>> GetOpenAsync(CancellationToken cancellationToken = default);
}
