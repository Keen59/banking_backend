using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces.Repositories;

public interface IPasswordResetTokenRepository : IRepository<PasswordResetToken>
{

}
