using MediatR;

namespace AuthService.Application.Commands.VerifyEnableTwoFactor;

public class VerifyEnableTwoFactorCommand : IRequest<VerifyEnableTwoFactorResponse>
{
    public Guid UserId { get; init; }

    public string Code { get; init; } = string.Empty;
}
