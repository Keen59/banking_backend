using RiskService.Domain.Entities;

namespace RiskService.Application.Interfaces.Repositories;

public interface ICustomerProfileRepository : IRepository<CustomerProfile>
{
    Task<bool> ExistsAsync(Guid customerId, CancellationToken cancellationToken = default);
}
