using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using MediatR;

namespace AuthService.Application.Commands.DisableTwoFactor;

public class DisableTwoFactorHandler(
    IUnitOfWork unitOfWork,
    IPasswordHasherService passwordHasher,
    IEmailOtpService emailOtpService) : IRequestHandler<DisableTwoFactorCommand, DisableTwoFactorResponse>
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

        var issued = await emailOtpService.IssueAsync(user, EOtpPurpose.TwoFactorDisable, cancellationToken);
        if (issued)
        {
            await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
            {
                UserId = user.Id,
                Action = EAuditAction.EmailOtpSent,
                Resource = "Authentication"
            }, cancellationToken);

            await unitOfWork.SaveAsync(cancellationToken);
        }

        return new DisableTwoFactorResponse
        {
            Message = "Doğrulama kodu e-posta adresinize gönderildi."
        };
    }
}
