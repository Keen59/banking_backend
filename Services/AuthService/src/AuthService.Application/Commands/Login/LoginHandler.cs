using AuthService.Application.DTOs.Authentication;
using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using MediatR;

namespace AuthService.Application.Commands.Login;

public class LoginHandler(
    IPasswordHasherService passwordHasher,
    IEmailOtpService emailOtpService,
    ILoginSessionService loginSessionService,
    IUnitOfWork unitOfWork) : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetRolesWithPermissionsByEmailAsync(request.Email, cancellationToken: cancellationToken);

        if (user is null)
        {
            await AddFailedLoginAttempt(request, null, ELoginFailureReason.UserNotFound, cancellationToken);
            await unitOfWork.SaveAsync(cancellationToken);

            throw new UnauthorizedAccessException("Geçersiz kullanıcı adı veya şifre.");
        }

        if (user.LockoutEnd.HasValue &&
            user.LockoutEnd.Value > DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("Hesap geçici olarak kilitlenmiştir.");
        }

        var passwordValid = passwordHasher.Verify(
            request.Password,
            user.PasswordHash);

        if (!passwordValid)
        {
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= 5)
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30);
            }

            unitOfWork.UserRepository.Update(user);

            await AddFailedLoginAttempt(request, user.Id, ELoginFailureReason.InvalidPassword, cancellationToken);

            await unitOfWork.SaveAsync(cancellationToken);

            throw new UnauthorizedAccessException("Geçersiz kullanıcı adı veya şifre.");
        }

        if (user.Status != EUserStatus.Active)
        {
            throw new UnauthorizedAccessException("Kullanıcı aktif değil.");
        }

        if (!user.IsEmailVerified)
        {
            throw new UnauthorizedAccessException("Email doğrulanmamış.");
        }

        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        unitOfWork.UserRepository.Update(user);

        var loginContext = new LoginContext
        {
            Email = request.Email,
            DeviceId = request.DeviceId,
            DeviceName = request.DeviceName,
            Browser = request.Browser,
            OperatingSystem = request.OperatingSystem,
            IpAddress = request.IpAddress
        };

        if (user.IsTwoFactorEnabled)
        {
            var issued = await emailOtpService.IssueAsync(user, EOtpPurpose.Login, cancellationToken);
            if (issued)
            {
                await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
                {
                    UserId = user.Id,
                    Action = EAuditAction.EmailOtpSent,
                    Resource = "Authentication",
                    IpAddress = request.IpAddress
                }, cancellationToken);
            }

            await unitOfWork.SaveAsync(cancellationToken);

            return new LoginResponse
            {
                RequiresTwoFactor = true,
                Message = "Doğrulama kodu e-posta adresinize gönderildi."
            };
        }

        return await loginSessionService.IssueSessionAsync(user, loginContext, cancellationToken);
    }

    private async Task AddFailedLoginAttempt(
        LoginCommand request,
        Guid? userId,
        ELoginFailureReason reason,
        CancellationToken cancellationToken)
    {
        var loginAttempt = new LoginAttempt
        {
            UserId = userId,
            Email = request.Email,
            IpAddress = request.IpAddress,
            Device = request.DeviceName ?? request.DeviceId,
            IsSuccessful = false,
            FailureReason = reason,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await unitOfWork.LoginAttemptRepository.AddAsync(loginAttempt, cancellationToken: cancellationToken);
    }
}
