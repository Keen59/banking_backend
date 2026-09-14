using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public class PasswordResetTokenRepository : Repository<PasswordResetToken>, IPasswordResetTokenRepository
{
    private readonly DBContext context;

    public PasswordResetTokenRepository(DBContext context) : base(context)
    {
        this.context = context;
    }

    public async Task<PasswordResetToken?> GetByHashedToken(string hashedToken, CancellationToken cancellationToken)
    {
        return await context.PasswordResetToken
            .FirstOrDefaultAsync(x => x.TokenHash == hashedToken, cancellationToken);
    }
}
