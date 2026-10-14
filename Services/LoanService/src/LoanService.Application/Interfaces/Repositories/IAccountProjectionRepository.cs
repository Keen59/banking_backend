using LoanService.Domain.Entities;

namespace LoanService.Application.Interfaces.Repositories;

public interface IAccountProjectionRepository : IRepository<AccountProjection>
{
    Task<bool> ExistsAsync(Guid accountId, CancellationToken cancellationToken = default);
}
