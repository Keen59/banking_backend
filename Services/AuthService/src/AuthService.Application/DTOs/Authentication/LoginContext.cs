namespace AuthService.Application.DTOs.Authentication;

public class LoginContext
{
    public string Email { get; init; } = string.Empty;

    public string DeviceId { get; init; } = string.Empty;

    public string? DeviceName { get; init; }

    public string? Browser { get; init; }

    public string? OperatingSystem { get; init; }

    public string IpAddress { get; init; } = string.Empty;
}
