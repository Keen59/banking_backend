using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Application.Commands.ChangePassword;

public class ChangePasswordHandler(
    IUnitOfWork unitOfWork,
    IPasswordHasherService passwordHasher) : IRequestHandler<ChangePasswordCommand, ChangePasswordResponse>
{
    public async Task<ChangePasswordResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByIdAsync(request.UserId)
            ?? throw new UnauthorizedAccessException("Oturum geçersiz.");

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Mevcut şifre hatalı.");

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

        return new ChangePasswordResponse
        {
            Message = "Şifre güncellendi. Yeniden giriş yapmanız gerekir."
        };
    }
}
