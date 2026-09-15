using AuthService.Application.Interfaces.Repositories;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using Banking.Contracts.Authorization;
using MediatR;

namespace AuthService.Application.Commands.GrantRole;

public class GrantRoleHandler(IUnitOfWork unitOfWork) : IRequestHandler<GrantRoleCommand, GrantRoleResponse>
{
    public async Task<GrantRoleResponse> Handle(GrantRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await unitOfWork.RoleRepository.GetByNameAsync(request.RoleName.Trim(), cancellationToken)
            ?? throw new InvalidOperationException("Rol bulunamadı.");

        var target = await unitOfWork.UserRepository.GetByIdAsync(request.TargetUserId)
            ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");

        if (role.Name == Roles.Operations)
        {
            var operationsCount = await unitOfWork.RoleRepository.CountUsersInRoleAsync(role.Id, cancellationToken);
            if (operationsCount > 0)
            {
                var actor = await unitOfWork.UserRepository.GetRolesWithPermissionsByIdAsync(
                    request.ActorUserId,
                    cancellationToken: cancellationToken) ?? throw new UnauthorizedAccessException("Oturum geçersiz.");

                var canAssign = actor.UserRoles
                    .SelectMany(x => x.Role.RolePermissions)
                    .Any(x => x.Permission.Code == Permissions.RolesAssign);

                if (!canAssign)
                    throw new UnauthorizedAccessException("Rol atama yetkiniz yok.");
            }
        }
        else if (request.ActorUserId != request.TargetUserId)
        {
            var actor = await unitOfWork.UserRepository.GetRolesWithPermissionsByIdAsync(
                request.ActorUserId,
                cancellationToken: cancellationToken) ?? throw new UnauthorizedAccessException("Oturum geçersiz.");

            var canAssign = actor.UserRoles
                .SelectMany(x => x.Role.RolePermissions)
                .Any(x => x.Permission.Code == Permissions.RolesAssign);

            if (!canAssign)
                throw new UnauthorizedAccessException("Rol atama yetkiniz yok.");
        }

        await unitOfWork.RoleRepository.AssignAsync(target.Id, role.Id, cancellationToken);
        await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            UserId = request.ActorUserId,
            Action = EAuditAction.RoleAssigned,
            Resource = "Authorization",
            Metadata = $"{{\"role\":\"{role.Name}\",\"targetUserId\":\"{target.Id}\"}}"
        }, cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        return new GrantRoleResponse
        {
            Message = $"{role.Name} rolü atandı. Yeni yetkiler için yeniden giriş yapın."
        };
    }
}
