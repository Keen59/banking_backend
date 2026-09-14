using AuthService.Domain.Enums;
using MediatR;

namespace AuthService.Application.Commands.VerifyEmailOtp;

public class VerifyEmailOtpCommand : IRequest<VerifyEmailOtpResponse>
{
    public string Email { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public EOtpPurpose Purpose { get; init; }
}
