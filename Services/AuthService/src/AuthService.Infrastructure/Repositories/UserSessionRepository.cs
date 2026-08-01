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

}