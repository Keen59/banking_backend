using LoanService.Application.Interfaces.Repositories;
using LoanService.Domain.Entities;
using LoanService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace LoanService.Infrastructure.Repositories;

public class AccountProjectionRepository(DBContext context)
    : Repository<AccountProjection>(context), IAccountProjectionRepository
{
    public async Task<bool> ExistsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await Context.AccountProjection.AnyAsync(x => x.Id == accountId, cancellationToken);
    }
}
