using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using MediatR;

namespace AuthService.Application.Commands.ForgotPassword;

public class ForgotPasswordHandler(
    IUnitOfWork unitOfWork,
    IEmailOtpService emailOtpService) : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByEmailAsync(request.Email, cancellationToken: cancellationToken);

        if (user is not null)
        {
            var issued = await emailOtpService.IssueAsync(user, EOtpPurpose.PasswordReset, cancellationToken);
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
        }

        return new ForgotPasswordResponse
        {
            IsSuccess = true,
            Message = "Şifre sıfırlama kodu e-posta adresinize gönderildi."
        };
    }
}
