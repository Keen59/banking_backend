using AuthService.Application.DTOs.Authentication;
using MediatR;

namespace AuthService.Application.Commands.GetCurrentUser;

public class GetCurrentUserCommand : IRequest<GetCurrentUserResponse>
{
    public Guid UserId { get; init; }
}

public class GetCurrentUserResponse : AuthService.Application.DTOs.Response
{
    public UserInfoDto? User { get; set; }
}
