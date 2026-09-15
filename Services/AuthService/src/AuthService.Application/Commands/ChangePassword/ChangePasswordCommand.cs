using MediatR;

namespace AuthService.Application.Commands.ChangePassword;

public class ChangePasswordCommand : IRequest<ChangePasswordResponse>
{
    public Guid UserId { get; init; }

    public string CurrentPassword { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
