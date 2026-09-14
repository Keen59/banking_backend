using AuthService.Application.Interfaces.Repositories;
using AuthService.Application.Interfaces.Services;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using MediatR;

namespace AuthService.Application.Commands.VerifyEmailOtp;

public class VerifyEmailOtpHandler(
    IUnitOfWork unitOfWork,
    IEmailOtpService emailOtpService) : IRequestHandler<VerifyEmailOtpCommand, VerifyEmailOtpResponse>
{
    public async Task<VerifyEmailOtpResponse> Handle(VerifyEmailOtpCommand request, CancellationToken cancellationToken)
    {
        var user = await emailOtpService.VerifyAndConsumeAsync(
            request.Email,
            request.Code,
            request.Purpose,
            cancellationToken);

        user.IsEmailVerified = true;
        unitOfWork.UserRepository.Update(user);

        await unitOfWork.AuditLogRepository.AddAsync(new AuditLog
        {
            UserId = user.Id,
            Action = EAuditAction.EmailVerified,
            Resource = "Authentication"
        }, cancellationToken);

        await unitOfWork.SaveAsync(cancellationToken);

        return new VerifyEmailOtpResponse
        {
            IsSuccess = true,
            Message = "E-posta adresi doğrulandı."
        };
    }
}
