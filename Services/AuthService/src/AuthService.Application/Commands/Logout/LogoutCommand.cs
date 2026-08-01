using MediatR;

namespace AuthService.Application.Commands.Logout;

public class LogoutCommand : IRequest<LogoutResponse>
{
    public string UserId { get; init; } = string.Empty; 
    public string SessionId { get; init; } = string.Empty;
}
