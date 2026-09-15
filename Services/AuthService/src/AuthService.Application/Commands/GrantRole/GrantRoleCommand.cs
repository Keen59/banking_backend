using MediatR;

namespace AuthService.Application.Commands.GrantRole;

public class GrantRoleCommand : IRequest<GrantRoleResponse>
{
    public Guid ActorUserId { get; init; }

    public Guid TargetUserId { get; init; }

    public string RoleName { get; init; } = string.Empty;
}
