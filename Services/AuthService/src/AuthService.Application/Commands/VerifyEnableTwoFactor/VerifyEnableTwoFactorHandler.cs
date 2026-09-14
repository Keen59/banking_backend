using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using MediatR;

namespace AuthService.Application.Commands.VerifyEnableTwoFactor;

public class VerifyEnableTwoFactorHandler(
    IUnitOfWork unitOfWork,
    IEmailOtpService emailOtpService) : IRequestHandler<VerifyEnableTwoFactorCommand, VerifyEnableTwoFactorResponse>
{
    public async Task<VerifyEnableTwoFactorResponse> Handle(
        VerifyEnableTwoFactorCommand request,
        CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByIdAsync(request.UserId)
            ?? throw new UnauthorizedAccessException("Oturum geçersiz.");

        await emailOtpService.VerifyAndConsumeAsync(
            user.Email,
            request.Code,
            EOtpPurpose.TwoFactorSetup,
            cancellationToken);

        user.IsTwoFactorEnabled = true;
        unitOfWork.UserRepository.Update(user);

        await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            UserId = user.Id,
            Action = EAuditAction.TwoFactorEnabled,
            Resource = "Authentication"
        }, cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        return new VerifyEnableTwoFactorResponse
        {
            Message = "İki faktörlü doğrulama açıldı."
        };
    }
}
