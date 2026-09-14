using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    private readonly DBContext context;

    public RefreshTokenRepository(DBContext context) : base(context)
    {
        this.context = context;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await context.RefreshToken
            .FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
    }

    public async Task<List<RefreshToken>> GetByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default)
    {
        return await context.RefreshToken
            .Where(x => x.FamilyId == familyId)
            .ToListAsync(cancellationToken);
    }
}
