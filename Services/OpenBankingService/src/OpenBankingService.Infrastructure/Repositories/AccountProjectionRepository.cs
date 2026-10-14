using OpenBankingService.Application.Interfaces.Repositories;
using OpenBankingService.Domain.Entities;
using OpenBankingService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace OpenBankingService.Infrastructure.Repositories;

public class AccountProjectionRepository(DBContext context)
    : Repository<AccountProjection>(context), IAccountProjectionRepository
{
    public async Task<bool> ExistsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await Context.AccountProjection.AnyAsync(x => x.Id == accountId, cancellationToken);
    }
}
