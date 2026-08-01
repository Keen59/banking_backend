using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    private readonly DBContext context;
    public UserRepository(DBContext context) : base(context)
    {
        this.context = context;
    }

    public async Task<User?> GetByEmailAsync(string email, bool asNoTracking = false, CancellationToken cancellationToken = default)
    {
        return await context.User.AsNoTracking().FirstOrDefaultAsync(d => d.Email == email);
    }

    public async Task<User?> GetRolesWithPermissionsByEmailAsync(
       string email,
       bool asNoTracking = false,
       CancellationToken cancellationToken = default)
    {
        IQueryable<User> query = context.User
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                    .ThenInclude(x => x.RolePermissions)
                        .ThenInclude(x => x.Permission);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            x => x.Email == email,
            cancellationToken);
    }
}
