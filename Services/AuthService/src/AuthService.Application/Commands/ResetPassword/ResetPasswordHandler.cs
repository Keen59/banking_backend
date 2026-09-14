using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Application.Commands.ResetPassword;

public class ResetPasswordHandler(
    IUnitOfWork unitOfWork,
    IPasswordHasherService passwordHasher,
    IEmailOtpService emailOtpService) : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await emailOtpService.VerifyAndConsumeAsync(
            request.Email,
            request.Token,
            EOtpPurpose.PasswordReset,
            cancellationToken);

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        unitOfWork.UserRepository.Update(user);

        var refreshTokens = await unitOfWork.RefreshTokenRepository.Query()
            .Where(x => x.UserId == user.Id && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var refreshToken in refreshTokens)
        {
            refreshToken.RevokedAt = DateTimeOffset.UtcNow;
        }

        var sessions = await unitOfWork.UserSessionRepository.Query()
            .Where(x => x.UserId == user.Id && x.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.RevokedAt = DateTimeOffset.UtcNow;
        }

        await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            UserId = user.Id,
            Action = EAuditAction.PasswordChanged,
            Resource = "Authentication"
        }, cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        return new ResetPasswordResponse
        {
            IsSuccess = true,
            Message = "Şifre başarıyla güncellendi."
        };
    }
}
