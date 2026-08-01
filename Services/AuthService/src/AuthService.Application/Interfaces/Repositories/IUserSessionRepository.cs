using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces.Repositories;
public interface IUserSessionRepository:IRepository<UserSession>
{
    public Task<UserSession?> GetSessionWithRefreshTokenById(Guid Id, bool asNoTracking = false, CancellationToken cancellationToken = default);
}