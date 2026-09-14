using MediatR;

namespace AuthService.Application.Commands.RefreshToken;

public class RefreshTokenCommand : IRequest<RefreshTokenResponse>
{
    public string RefreshToken { get; init; } = string.Empty;

    public string IpAddress { get; init; } = string.Empty;
}
