using AuthService.Application.DTOs;

namespace AuthService.Application.Commands.ForgotPassword;

public class ForgotPasswordResponse : Response
{
    public string ResetToken { get; internal set; }
}
