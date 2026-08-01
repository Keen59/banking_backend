using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;

namespace AuthService.Infrastructure.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        public Task<RefreshToken> Generate(Guid Id)
        {
            throw new NotImplementedException();
        }
    }
}
