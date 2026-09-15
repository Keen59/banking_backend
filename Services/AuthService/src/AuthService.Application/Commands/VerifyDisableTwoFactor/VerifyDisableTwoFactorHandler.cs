using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using MediatR;

namespace AuthService.Application.Commands.VerifyDisableTwoFactor;

public class VerifyDisableTwoFactorHandler(
    IUnitOfWork unitOfWork,
    IEmailOtpService emailOtpService) : IRequestHandler<VerifyDisableTwoFactorCommand, VerifyDisableTwoFactorResponse>
{
    public async Task<VerifyDisableTwoFactorResponse> Handle(
        VerifyDisableTwoFactorCommand request,
        CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByIdAsync(request.UserId)
            ?? throw new UnauthorizedAccessException("Oturum geçersiz.");

        if (!user.IsTwoFactorEnabled)
        {
            return new VerifyDisableTwoFactorResponse
            {
                Message = "İki faktörlü doğrulama zaten kapalı."
            };
        }

        await emailOtpService.VerifyAndConsumeAsync(
            user.Email,
            request.Code,
            EOtpPurpose.TwoFactorDisable,
            cancellationToken);

        user.IsTwoFactorEnabled = false;
        unitOfWork.UserRepository.Update(user);

        await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            UserId = user.Id,
            Action = EAuditAction.TwoFactorDisabled,
            Resource = "Authentication"
        }, cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        return new VerifyDisableTwoFactorResponse
        {
            Message = "İki faktörlü doğrulama kapatıldı."
        };
    }
}
