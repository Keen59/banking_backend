using AuthService.Application.DTOs.Authentication;

namespace AuthService.Application.Interfaces.Services;

public interface IRefreshTokenService
{
    Task<IssuedRefreshToken> Generate(Guid userId, Guid? familyId = null);

    string Hash(string plaintext);

    void RememberRotation(string previousTokenHash, string newPlaintext);

    string? TryGetRotatedPlaintext(string previousTokenHash);
}
