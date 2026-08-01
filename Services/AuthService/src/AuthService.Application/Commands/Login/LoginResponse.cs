namespace AuthService.Application.DTOs.Authentication;

public class LoginResponse:Response
{
    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public DateTimeOffset? AccessTokenExpiresAt { get; set; }

    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    // 2FA gerekiyorsa true döner.
    public bool RequiresTwoFactor { get; set; }

    // Giriş yapan kullanıcının temel bilgileri
    public UserInfoDto? User { get; set; }
}