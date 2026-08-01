using AuthService.Application.DTOs.Authentication;
using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces.Services;
public interface IJwtService
{
    Task<AccessTokenDto?> GenerateAccessToken(User user, UserSession session,
       IEnumerable<Permission> permissions);
}