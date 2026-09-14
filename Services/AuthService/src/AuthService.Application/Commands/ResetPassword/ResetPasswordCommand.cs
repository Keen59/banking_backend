using MediatR;

namespace AuthService.Application.Commands.ResetPassword;

public class ResetPasswordCommand:IRequest<ResetPasswordResponse>
{
    public string Email { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}
