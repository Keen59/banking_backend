using AuthService.Application.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Commands.VerifyLoginOtp;

public class VerifyLoginOtpCommand : IRequest<LoginResponse>
{
    public string Email { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public string DeviceId { get; init; } = string.Empty;

    public string? DeviceName { get; init; }

    public string? Browser { get; init; }

    public string? OperatingSystem { get; init; }

    public string IpAddress { get; init; } = string.Empty;
}
