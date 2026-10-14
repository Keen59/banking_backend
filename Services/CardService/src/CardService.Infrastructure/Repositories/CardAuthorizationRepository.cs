using CardService.Application.Interfaces.Repositories;
using CardService.Domain.Entities;
using CardService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace CardService.Infrastructure.Repositories;

public class CardAuthorizationRepository(DBContext context)
    : Repository<CardAuthorization>(context), ICardAuthorizationRepository
{
    public async Task<CardAuthorization?> GetByIdempotencyKeyAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await Context.CardAuthorization.FirstOrDefaultAsync(
            x => x.CustomerId == customerId && x.IdempotencyKey == idempotencyKey,
            cancellationToken);
    }

    public async Task<IReadOnlyList<CardAuthorization>> GetByCardIdAsync(
        Guid cardId,
        CancellationToken cancellationToken = default)
    {
        return await Context.CardAuthorization
            .AsNoTracking()
            .Where(x => x.CardId == cardId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
