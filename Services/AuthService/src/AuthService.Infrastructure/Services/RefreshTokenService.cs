using System.Security.Cryptography;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace AuthService.Infrastructure.Services;

public class RefreshTokenService(IConfiguration configuration) : IRefreshTokenService
{
    public Task<RefreshToken> Generate(Guid userId, Guid? familyId = null)
    {
        var expirationDays = int.TryParse(configuration["JwtSettings:RefreshTokenExpirationDays"], out var days)
            ? days
            : 7;
        var tokenBytes = RandomNumberGenerator.GetBytes(64);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FamilyId = familyId ?? Guid.NewGuid(),
            Token = Convert.ToBase64String(tokenBytes),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(expirationDays),
            CreatedIp = string.Empty
        };

        return Task.FromResult(refreshToken);
    }
}
