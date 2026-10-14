using OpenBankingService.Application.Interfaces.Repositories;
using OpenBankingService.Domain.Entities;
using OpenBankingService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace OpenBankingService.Infrastructure.Repositories;

public class ConsentRepository(DBContext context) : Repository<Consent>(context), IConsentRepository
{
    public async Task<Consent?> GetByIdempotencyKeyAsync(
        Guid customerId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await Context.Consent
            .Include(x => x.Accounts)
            .FirstOrDefaultAsync(
                x => x.CustomerId == customerId && x.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public async Task<Consent?> GetByAccessTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return await Context.Consent
            .Include(x => x.Accounts)
            .FirstOrDefaultAsync(x => x.AccessToken == accessToken, cancellationToken);
    }

    public async Task<Consent?> GetByIdWithAccountsAsync(
        Guid consentId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Consent
            .Include(x => x.Accounts)
            .FirstOrDefaultAsync(x => x.Id == consentId, cancellationToken);
    }

    public async Task<IReadOnlyList<Consent>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Consent
            .AsNoTracking()
            .Include(x => x.Accounts)
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
