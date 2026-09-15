using AuthService.Application.DTOs;

namespace AuthService.Application.Commands.Register;

public class RegisterResponse : Response
{
    public Guid UserId { get; set; }
}
