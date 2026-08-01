namespace AuthService.Application.DTOs.Authentication;

public class AccessTokenDto
{
    public string Token { get; set; } = null!;

    public DateTimeOffset ExpiresAt { get; set; }
}