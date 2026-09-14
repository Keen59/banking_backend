using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    public Task<User?> GetByEmailAsync(string email, bool asNoTracking = false, CancellationToken cancellationToken = default);
    public Task<User?> GetRolesWithPermissionsByEmailAsync(string email, bool asNoTracking = false, CancellationToken cancellationToken = default);
    public Task<User?> GetRolesWithPermissionsByIdAsync(Guid userId, bool asNoTracking = false, CancellationToken cancellationToken = default);
}
