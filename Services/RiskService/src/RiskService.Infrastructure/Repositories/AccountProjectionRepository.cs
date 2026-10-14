using RiskService.Application.Interfaces.Repositories;
using RiskService.Domain.Entities;
using RiskService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace RiskService.Infrastructure.Repositories;

public class AccountProjectionRepository(DBContext context)
    : Repository<AccountProjection>(context), IAccountProjectionRepository
{
    public async Task<bool> ExistsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await Context.AccountProjection.AnyAsync(x => x.Id == accountId, cancellationToken);
    }
}
