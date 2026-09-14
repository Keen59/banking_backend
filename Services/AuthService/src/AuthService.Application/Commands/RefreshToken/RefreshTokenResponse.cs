using AuthService.Application.DTOs;
using AuthService.Application.DTOs.Authentication;

namespace AuthService.Application.Commands.RefreshToken;

public class RefreshTokenResponse : Response
{
    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public DateTimeOffset? AccessTokenExpiresAt { get; set; }

    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    public UserInfoDto? User { get; set; }
}
