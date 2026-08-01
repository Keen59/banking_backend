using AuthService.Domain.Entities;
using AuthService.Infrastructure.Context;
using AuthService.Infrastructure.Repositories;

namespace AuthService.Application.Interfaces.Repositories;

public class PasswordResetTokenRepository : Repository<PasswordResetToken>, IPasswordResetTokenRepository
{
    public PasswordResetTokenRepository(DBContext context) : base(context)
    {
    }
}
