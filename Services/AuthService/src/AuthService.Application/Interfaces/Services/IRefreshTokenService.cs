using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces.Services;

public interface IRefreshTokenService 
{
   Task<RefreshToken> Generate(Guid Id);
}
