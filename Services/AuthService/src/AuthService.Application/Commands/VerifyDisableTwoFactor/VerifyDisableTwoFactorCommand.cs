using MediatR;

namespace AuthService.Application.Commands.VerifyDisableTwoFactor;

public class VerifyDisableTwoFactorCommand : IRequest<VerifyDisableTwoFactorResponse>
{
    public Guid UserId { get; init; }

    public string Code { get; init; } = string.Empty;
}
