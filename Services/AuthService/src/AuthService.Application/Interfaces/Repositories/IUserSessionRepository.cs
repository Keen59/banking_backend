using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces.Repositories;
public interface IUserSessionRepository:IRepository<UserSession>
{
    public Task<UserSession?> GetSessionWithRefreshTokenById(Guid Id, bool asNoTracking = false, CancellationToken cancellationToken = default);
    public Task<UserSession?> GetActiveByRefreshTokenIdAsync(Guid refreshTokenId, CancellationToken cancellationToken = default);
    public Task<List<UserSession>> GetActiveByRefreshTokenIdsAsync(IReadOnlyCollection<Guid> refreshTokenIds, CancellationToken cancellationToken = default);
}