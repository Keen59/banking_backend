using AuthService.Application.DTOs.Authentication;
using AuthService.Application.Interfaces.Repositories;
using MediatR;

namespace AuthService.Application.Commands.GetCurrentUser;

public class GetCurrentUserHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetCurrentUserCommand, GetCurrentUserResponse>
{
    public async Task<GetCurrentUserResponse> Handle(GetCurrentUserCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetRolesWithPermissionsByIdAsync(
            request.UserId,
            asNoTracking: true,
            cancellationToken: cancellationToken) ?? throw new UnauthorizedAccessException("Oturum geçersiz.");

        return new GetCurrentUserResponse
        {
            Message = "Kullanıcı bilgisi.",
            User = new UserInfoDto
            {
                Id = user.Id,
                CustomerId = user.CustomerId,
                Email = user.Email,
                Username = user.Username,
                Roles = user.UserRoles.Select(x => x.Role.Name).ToList(),
                IsEmailVerified = user.IsEmailVerified,
                IsTwoFactorEnabled = user.IsTwoFactorEnabled
            }
        };
    }
}
