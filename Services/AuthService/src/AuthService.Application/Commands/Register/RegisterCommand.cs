using MediatR;

namespace AuthService.Application.Commands.Register;

public class RegisterCommand : IRequest<RegisterResponse>
{
    public string Email { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;
}
