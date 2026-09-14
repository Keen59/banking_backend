using MediatR;

namespace AuthService.Application.Commands.DisableTwoFactor;

public class DisableTwoFactorCommand : IRequest<DisableTwoFactorResponse>
{
    public Guid UserId { get; init; }

    public string Password { get; init; } = string.Empty;
}
