using AuthService.Domain.Enums;
using MediatR;

namespace AuthService.Application.Commands.SendEmailOtp;

public class SendEmailOtpCommand : IRequest<SendEmailOtpResponse>
{
    public string Email { get; init; } = string.Empty;

    public EOtpPurpose Purpose { get; init; }
}
