using RiskService.Application.Interfaces.Repositories;
using RiskService.Domain.Entities;
using RiskService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace RiskService.Infrastructure.Repositories;

public class CustomerProfileRepository(DBContext context)
    : Repository<CustomerProfile>(context), ICustomerProfileRepository
{
    public async Task<bool> ExistsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await Context.CustomerProfile.AnyAsync(x => x.Id == customerId, cancellationToken);
    }
}
