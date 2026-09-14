using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;
public class UserSessionRepository : Repository<UserSession>, IUserSessionRepository
{
 
    private readonly DBContext context;
    public UserSessionRepository(DBContext context) : base(context)
    {
        this.context = context;
    }
    public async Task<UserSession?> GetSessionWithRefreshTokenById(Guid Id, bool asNoTracking = false, CancellationToken cancellationToken = default)
    {
        IQueryable<UserSession> query = context.UserSession
            .Include(x => x.RefreshToken);

        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(
            x => x.Id ==Id,
            cancellationToken);
    }

    public async Task<UserSession?> GetActiveByRefreshTokenIdAsync(
        Guid refreshTokenId,
        CancellationToken cancellationToken = default)
    {
        return await context.UserSession
            .Include(x => x.RefreshToken)
            .FirstOrDefaultAsync(
                x => x.RefreshTokenId == refreshTokenId && x.IsActive,
                cancellationToken);
    }

    public async Task<List<UserSession>> GetActiveByRefreshTokenIdsAsync(
        IReadOnlyCollection<Guid> refreshTokenIds,
        CancellationToken cancellationToken = default)
    {
        if (refreshTokenIds.Count == 0)
        {
            return [];
        }

        return await context.UserSession
            .Where(x => x.IsActive && refreshTokenIds.Contains(x.RefreshTokenId))
            .ToListAsync(cancellationToken);
    }
}