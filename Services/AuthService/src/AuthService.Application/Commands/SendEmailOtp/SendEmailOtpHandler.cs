using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using MediatR;

namespace AuthService.Application.Commands.SendEmailOtp;

public class SendEmailOtpHandler(
    IUnitOfWork unitOfWork,
    IEmailOtpService emailOtpService) : IRequestHandler<SendEmailOtpCommand, SendEmailOtpResponse>
{
    public async Task<SendEmailOtpResponse> Handle(SendEmailOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByEmailAsync(request.Email, cancellationToken: cancellationToken);

        if (user is not null &&
            (request.Purpose != EOtpPurpose.EmailVerification || !user.IsEmailVerified))
        {
            var issued = await emailOtpService.IssueAsync(user, request.Purpose, cancellationToken);
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

        return new SendEmailOtpResponse
        {
            IsSuccess = true,
            Message = "Doğrulama kodu e-posta adresinize gönderildi."
        };
    }
}
