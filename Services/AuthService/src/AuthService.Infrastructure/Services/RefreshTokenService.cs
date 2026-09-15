using System.Collections.Concurrent;
using System.Security.Cryptography;
using AuthService.Application.DTOs.Authentication;
using AuthService.Application.Helpers;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace AuthService.Infrastructure.Services;

public class RefreshTokenService(IConfiguration configuration) : IRefreshTokenService
{
    private static readonly TimeSpan RotationGracePeriod = TimeSpan.FromSeconds(15);
    private readonly ConcurrentDictionary<string, (string Plaintext, DateTimeOffset ExpiresAt)> _rotationGrace = new();

    public Task<IssuedRefreshToken> Generate(Guid userId, Guid? familyId = null)
    {
        var expirationDays = int.TryParse(configuration["JwtSettings:RefreshTokenExpirationDays"], out var days)
            ? days
            : 7;
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var plaintext = Convert.ToBase64String(tokenBytes);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FamilyId = familyId ?? Guid.NewGuid(),
            Token = Hash(plaintext),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(expirationDays),
            CreatedIp = string.Empty
        };

        return Task.FromResult(new IssuedRefreshToken(refreshToken, plaintext));
    }

    public string Hash(string plaintext)
    {
        var secret = configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey yapılandırılmamış.");

        return GenerateTokenHelper.ComputeHmacSha256(secret, plaintext);
    }

    public void RememberRotation(string previousTokenHash, string newPlaintext)
    {
        _rotationGrace[previousTokenHash] = (newPlaintext, DateTimeOffset.UtcNow.Add(RotationGracePeriod));
    }

    public string? TryGetRotatedPlaintext(string previousTokenHash)
    {
        if (!_rotationGrace.TryGetValue(previousTokenHash, out var entry))
            return null;

        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _rotationGrace.TryRemove(previousTokenHash, out _);
            return null;
        }

        return entry.Plaintext;
    }
}
