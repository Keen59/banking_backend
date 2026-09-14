using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces.Repositories;

public interface IPasswordResetTokenRepository : IRepository<PasswordResetToken>
{
    Task<PasswordResetToken?> GetByHashedToken(string hashedToken, CancellationToken cancellationToken);
}
