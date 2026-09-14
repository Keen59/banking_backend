using MediatR;

namespace AuthService.Application.Commands.EnableTwoFactor;

public class EnableTwoFactorCommand : IRequest<EnableTwoFactorResponse>
{
    public Guid UserId { get; init; }
}
