using PaymentService.Application.Interfaces.Repositories;
using PaymentService.Domain.Entities;
using PaymentService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Infrastructure.Repositories;

public class AccountProjectionRepository(DBContext context)
    : Repository<AccountProjection>(context), IAccountProjectionRepository
{
    public async Task<bool> ExistsAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await Context.AccountProjection.AnyAsync(x => x.Id == accountId, cancellationToken);
    }
}
