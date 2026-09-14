using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces.Services;

public interface IRefreshTokenService
{
    Task<RefreshToken> Generate(Guid userId, Guid? familyId = null);
}
