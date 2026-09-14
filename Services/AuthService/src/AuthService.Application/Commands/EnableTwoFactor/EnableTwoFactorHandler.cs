using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using MediatR;

namespace AuthService.Application.Commands.EnableTwoFactor;

public class EnableTwoFactorHandler(
    IUnitOfWork unitOfWork,
    IEmailOtpService emailOtpService) : IRequestHandler<EnableTwoFactorCommand, EnableTwoFactorResponse>
{
    public async Task<EnableTwoFactorResponse> Handle(EnableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByIdAsync(request.UserId)
            ?? throw new UnauthorizedAccessException("Oturum geçersiz.");

        if (!user.IsEmailVerified)
            throw new UnauthorizedAccessException("Email doğrulanmamış.");

        if (user.IsTwoFactorEnabled)
        {
            return new EnableTwoFactorResponse
            {
                Message = "İki faktörlü doğrulama zaten açık."
            };
        }

        var issued = await emailOtpService.IssueAsync(user, EOtpPurpose.TwoFactorSetup, cancellationToken);
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

        return new EnableTwoFactorResponse
        {
            Message = "Doğrulama kodu e-posta adresinize gönderildi."
        };
    }
}
