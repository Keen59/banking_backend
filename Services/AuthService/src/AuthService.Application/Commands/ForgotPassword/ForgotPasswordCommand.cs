using MediatR;

namespace AuthService.Application.Commands.ForgotPassword;

public class ForgotPasswordCommand : IRequest<ForgotPasswordResponse>
{
    public string Email { get; init; } = string.Empty;
}
