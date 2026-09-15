using AuthService.Application.DTOs.Authentication;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using NSubstitute;

namespace AuthService.UnitTests;

internal static class AuthFakes
{
    public static User ActiveUser(Guid? id = null, bool twoFactor = false) => new()
    {
        Id = id ?? Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        Email = "user@example.com",
        Username = "user",
        PhoneNumber = "5550000000",
        PasswordHash = "hashed",
        IsEmailVerified = true,
        IsTwoFactorEnabled = twoFactor,
        Status = EUserStatus.Active,
        UserRoles = []
    };

    public static IUnitOfWork UnitOfWork(
        IUserRepository? users = null,
        IRefreshTokenRepository? refreshTokens = null,
        IUserSessionRepository? sessions = null,
        ILoginAttemptRepository? loginAttempts = null,
        IAuditLogRepository? auditLogs = null)
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.UserRepository.Returns(users ?? Substitute.For<IUserRepository>());
        unitOfWork.RefreshTokenRepository.Returns(refreshTokens ?? Substitute.For<IRefreshTokenRepository>());
        unitOfWork.UserSessionRepository.Returns(sessions ?? Substitute.For<IUserSessionRepository>());
        unitOfWork.LoginAttemptRepository.Returns(loginAttempts ?? Substitute.For<ILoginAttemptRepository>());
        unitOfWork.AuditLogRepository.Returns(auditLogs ?? Substitute.For<IAuditLogRepository>());
        return unitOfWork;
    }

    public static AccessTokenDto AccessToken() => new()
    {
        Token = "access-token",
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(60)
    };
}
