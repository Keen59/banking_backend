namespace AuthService.Application.DTOs.Authentication;

public class UserInfoDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public IReadOnlyCollection<string> Roles { get; set; } = [];
}