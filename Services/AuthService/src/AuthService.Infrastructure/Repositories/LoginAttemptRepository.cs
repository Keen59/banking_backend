using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Context;
namespace AuthService.Infrastructure.Repositories;

public class LoginAttemptRepository : Repository<LoginAttempt>, ILoginAttemptRepository
{
    public LoginAttemptRepository(DBContext context) : base(context)
    {
    }
}