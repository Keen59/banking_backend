using AuthService.Application.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Commands.Login;

public class LoginCommand : IRequest<LoginResponse>
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string DeviceId { get; init; } = string.Empty;

    public string? DeviceName { get; init; }

    public string? Browser { get; init; }

    public string? OperatingSystem { get; init; }

    public string IpAddress { get; init; } = string.Empty;
}
