using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces.Repositories;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<int> CountUsersInRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<bool> UserHasRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    Task AssignAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}
