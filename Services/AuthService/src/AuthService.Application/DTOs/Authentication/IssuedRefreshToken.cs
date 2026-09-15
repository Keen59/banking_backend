using AuthService.Domain.Entities;

namespace AuthService.Application.DTOs.Authentication;

public sealed record IssuedRefreshToken(RefreshToken Entity, string Plaintext);
