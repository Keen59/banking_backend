using AuthService.Application.DTOs.Authentication;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;

namespace AuthService.Infrastructure.Services;

public class LoginSessionService(
    IUnitOfWork unitOfWork,
    IJwtService jwtService,
    IRefreshTokenService refreshTokenService) : ILoginSessionService
{
    public async Task<LoginResponse> IssueSessionAsync(
        User user,
        LoginContext context,
        CancellationToken cancellationToken = default)
    {
        var fullUser = await unitOfWork.UserRepository.GetRolesWithPermissionsByIdAsync(
            user.Id,
            cancellationToken: cancellationToken) ?? user;

        var device = await unitOfWork.DeviceRepository.GetByIdentifierAsync(context.DeviceId);

        if (device is null || device.UserId != fullUser.Id)
        {
            device = new Device
            {
                Id = Guid.NewGuid(),
                UserId = fullUser.Id,
                DeviceIdentifier = context.DeviceId,
                DeviceName = context.DeviceName ?? "",
                Browser = context.Browser ?? "",
                OperatingSystem = context.OperatingSystem ?? "",
                IpAddress = context.IpAddress,
                LastLoginAt = DateTimeOffset.UtcNow,
                IsTrusted = false
            };

            await unitOfWork.DeviceRepository.AddAsync(device, cancellationToken);
        }
        else
        {
            device.LastLoginAt = DateTimeOffset.UtcNow;
            device.IpAddress = context.IpAddress;
            device.DeviceName = context.DeviceName ?? device.DeviceName;
            device.Browser = context.Browser ?? device.Browser;
            device.OperatingSystem = context.OperatingSystem ?? device.OperatingSystem;
            unitOfWork.DeviceRepository.Update(device);
        }

        var issuedRefreshToken = await refreshTokenService.Generate(fullUser.Id);
        issuedRefreshToken.Entity.CreatedIp = context.IpAddress;
        issuedRefreshToken.Entity.DeviceId = device.Id;

        await unitOfWork.RefreshTokenRepository.AddAsync(issuedRefreshToken.Entity, cancellationToken);

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = fullUser.Id,
            RefreshTokenId = issuedRefreshToken.Entity.Id,
            JwtId = Guid.NewGuid(),
            DeviceId = device.Id,
            IpAddress = context.IpAddress,
            ExpiresAt = issuedRefreshToken.Entity.ExpiresAt,
            LastActivityAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        await unitOfWork.UserSessionRepository.AddAsync(session, cancellationToken);

        var permissions = fullUser.UserRoles
            .SelectMany(x => x.Role.RolePermissions)
            .Select(x => x.Permission)
            .DistinctBy(x => x.Id)
            .ToList();

        var accessToken = await jwtService.GenerateAccessToken(fullUser, session, permissions)
            ?? throw new InvalidOperationException("Access token üretilemedi.");

        await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            UserId = fullUser.Id,
            Action = EAuditAction.Login,
            Resource = "Authentication",
            IpAddress = context.IpAddress,
            Device = context.DeviceName ?? ""
        }, cancellationToken);

        await unitOfWork.LoginAttemptRepository.AddAsync(new LoginAttempt
        {
            UserId = fullUser.Id,
            Email = context.Email,
            IpAddress = context.IpAddress,
            Device = context.DeviceName ?? "",
            IsSuccessful = true
        }, cancellationToken);

        unitOfWork.UserRepository.Update(fullUser);
        await unitOfWork.SaveAsync(cancellationToken);

        return new LoginResponse
        {
            Message = "Giriş başarılı.",
            AccessToken = accessToken.Token,
            RefreshToken = issuedRefreshToken.Plaintext,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshTokenExpiresAt = issuedRefreshToken.Entity.ExpiresAt,
            User = new UserInfoDto
            {
                Id = fullUser.Id,
                CustomerId = fullUser.CustomerId,
                Email = fullUser.Email,
                Username = fullUser.Username,
                Roles = fullUser.UserRoles.Select(x => x.Role.Name).ToList(),
                IsEmailVerified = fullUser.IsEmailVerified,
                IsTwoFactorEnabled = fullUser.IsTwoFactorEnabled
            }
        };
    }
}
