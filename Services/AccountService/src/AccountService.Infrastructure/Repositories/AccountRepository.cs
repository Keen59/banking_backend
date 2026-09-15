using AccountService.Application.Interfaces.Repositories;
using AccountService.Domain.Entities;
using AccountService.Domain.Enums;
using AccountService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Infrastructure.Repositories;

public class AccountRepository(DBContext context) : Repository<Account>(context), IAccountRepository
{
    public async Task<IReadOnlyList<Account>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Account
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid customerId,
        EAccountProductType productType,
        string currency,
        CancellationToken cancellationToken = default)
    {
        return await Context.Account.AnyAsync(
            x => x.CustomerId == customerId &&
                 x.ProductType == productType &&
                 x.Currency == currency,
            cancellationToken);
    }

    public async Task<bool> IbanExistsAsync(string iban, CancellationToken cancellationToken = default)
    {
        return await Context.Account.AnyAsync(x => x.Iban == iban, cancellationToken);
    }
}
