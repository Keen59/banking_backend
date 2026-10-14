using CardService.Application.Interfaces.Repositories;
using CardService.Domain.Entities;
using CardService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace CardService.Infrastructure.Repositories;

public class CardRepository(DBContext context) : Repository<Card>(context), ICardRepository
{
    public async Task<IReadOnlyList<Card>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Card
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Card.AnyAsync(x => x.AccountId == accountId, cancellationToken);
    }
}
