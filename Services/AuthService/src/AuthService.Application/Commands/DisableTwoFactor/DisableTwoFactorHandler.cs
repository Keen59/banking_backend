using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using MediatR;

namespace AuthService.Application.Commands.DisableTwoFactor;

public class DisableTwoFactorHandler(
    IUnitOfWork unitOfWork,
    IPasswordHasherService passwordHasher) : IRequestHandler<DisableTwoFactorCommand, DisableTwoFactorResponse>
{
    public async Task<DisableTwoFactorResponse> Handle(
        DisableTwoFactorCommand request,
        CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByIdAsync(request.UserId)
            ?? throw new UnauthorizedAccessException("Oturum geçersiz.");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Geçersiz şifre.");

        if (!user.IsTwoFactorEnabled)
        {
            return new DisableTwoFactorResponse
            {
                Message = "İki faktörlü doğrulama zaten kapalı."
            };
        }

        user.IsTwoFactorEnabled = false;
        unitOfWork.UserRepository.Update(user);

        await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            UserId = user.Id,
            Action = EAuditAction.TwoFactorDisabled,
            Resource = "Authentication"
        }, cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        return new DisableTwoFactorResponse
        {
            Message = "İki faktörlü doğrulama kapatıldı."
        };
    }
}
