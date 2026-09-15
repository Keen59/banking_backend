using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public class RoleRepository : Repository<Role>, IRoleRepository
{
    public RoleRepository(DBContext context) : base(context)
    {
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Role.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
    }

    public async Task<int> CountUsersInRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRole.CountAsync(x => x.RoleId == roleId, cancellationToken);
    }

    public async Task<bool> UserHasRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.UserRole.AnyAsync(x => x.UserId == userId && x.RoleId == roleId, cancellationToken);
    }

    public async Task AssignAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        if (await UserHasRoleAsync(userId, roleId, cancellationToken))
            return;

        await _context.UserRole.AddAsync(new UserRole
        {
            UserId = userId,
            RoleId = roleId
        }, cancellationToken);
    }
}
