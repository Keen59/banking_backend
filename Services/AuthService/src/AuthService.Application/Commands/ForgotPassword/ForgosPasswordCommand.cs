using MediatR;

namespace AuthService.Application.Commands.ForgotPassword;

public class ForgosPasswordCommand:IRequest<ForgotPasswordResponse>
{
    public string Email { get; init; } = string.Empty;
}
